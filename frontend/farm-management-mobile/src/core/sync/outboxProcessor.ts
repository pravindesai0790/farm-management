import { Platform } from 'react-native';
import axios, { AxiosError } from 'axios';
import { getDatabase } from '../db/database';
import { apiClient } from '../api/client';

/**
 * Robust UUIDv4 generator operating across Hermes, JSC, and Web runtimes.
 * Used for client-generated primary keys and idempotency keys.
 */
export function generateUuid(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

/**
 * Mutation record structure representing an queued outbound HTTP mutation.
 */
export interface OutboxMutation {
  id: string;
  operation_type: 'WORKER_CREATE' | 'WORKER_ASSIGN' | 'ATTENDANCE_DRAFT' | 'ATTENDANCE_FINALIZE';
  endpoint: string;
  http_method: 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  idempotency_key: string;
  payload_json: string;
  dependency_mutation_id?: string | null;
  status: 'PENDING' | 'SYNCING' | 'COMPLETED' | 'FAILED';
  retry_count: number;
  last_error?: string | null;
  created_at: string;
  synced_at?: string | null;
}

/**
 * Parameter structure for queueing an outbound mutation.
 */
export interface EnqueueMutationParams {
  operationType: OutboxMutation['operation_type'];
  endpoint: string;
  httpMethod: OutboxMutation['http_method'];
  idempotencyKey?: string;
  payload: Record<string, unknown>;
  dependencyMutationId?: string | null;
}

/**
 * In-memory fallback outbox queue for web browser preview when native SQLite is unavailable.
 */
const webOutboxStore: OutboxMutation[] = [];

/**
 * Enqueues a new mutation into the outbox table for atomic offline processing.
 * Generates a unique UUIDv4 idempotency key if one is not provided.
 */
export function enqueueMutation(params: EnqueueMutationParams): OutboxMutation {
  const id = generateUuid();
  const idempotencyKey = params.idempotencyKey || generateUuid();
  const now = new Date().toISOString();
  const payloadJson = JSON.stringify(params.payload);

  const mutation: OutboxMutation = {
    id,
    operation_type: params.operationType,
    endpoint: params.endpoint,
    http_method: params.httpMethod,
    idempotency_key: idempotencyKey,
    payload_json: payloadJson,
    dependency_mutation_id: params.dependencyMutationId ?? null,
    status: 'PENDING',
    retry_count: 0,
    last_error: null,
    created_at: now,
    synced_at: null,
  };

  if (Platform.OS === 'web') {
    webOutboxStore.push(mutation);
    return mutation;
  }

  const db = getDatabase();
  if (!db) {
    throw new Error('Database is not initialized. Cannot enqueue mutation.');
  }

  db.runSync(
    `INSERT INTO outbox_mutations (
      id, operation_type, endpoint, http_method, idempotency_key,
      payload_json, dependency_mutation_id, status, retry_count,
      last_error, created_at, synced_at
    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);`,
    [
      mutation.id,
      mutation.operation_type,
      mutation.endpoint,
      mutation.http_method,
      mutation.idempotency_key,
      mutation.payload_json,
      mutation.dependency_mutation_id ?? null,
      mutation.status,
      mutation.retry_count,
      mutation.last_error ?? null,
      mutation.created_at,
      mutation.synced_at ?? null,
    ]
  );

  return mutation;
}

/**
 * Retrieves all pending outbox mutations ordered sequentially by creation time.
 */
export function getPendingMutations(): OutboxMutation[] {
  if (Platform.OS === 'web') {
    return webOutboxStore.filter((m) => m.status === 'PENDING' || m.status === 'SYNCING');
  }

  const db = getDatabase();
  if (!db) return [];

  try {
    return db.getAllSync<OutboxMutation>(
      `SELECT * FROM outbox_mutations 
       WHERE status = 'PENDING' OR status = 'SYNCING'
       ORDER BY created_at ASC;`
    );
  } catch (err) {
    console.warn('[outboxProcessor] Error fetching pending mutations:', err);
    return [];
  }
}

/**
 * Returns the count of pending outbox mutations awaiting synchronization.
 */
export function getPendingOutboxCount(): number {
  if (Platform.OS === 'web') {
    return webOutboxStore.filter((m) => m.status === 'PENDING' || m.status === 'SYNCING').length;
  }

  const db = getDatabase();
  if (!db) return 0;

  try {
    const res = db.getAllSync<{ count: number }>(
      `SELECT COUNT(*) as count FROM outbox_mutations 
       WHERE status = 'PENDING' OR status = 'SYNCING';`
    );
    return res[0]?.count ?? 0;
  } catch (err) {
    console.warn('[outboxProcessor] Error counting pending mutations:', err);
    return 0;
  }
}

/**
 * Purges all pending or failed mutations from SQLite.
 * Useful for recovering when an invalid mutation is blocking the queue.
 */
export function clearFailedMutations(): number {
  if (Platform.OS === 'web') {
    const count = webOutboxStore.length;
    webOutboxStore.length = 0;
    return count;
  }

  const db = getDatabase();
  if (!db) return 0;

  try {
    const res = db.getAllSync<{ count: number }>(
      "SELECT COUNT(*) as count FROM outbox_mutations WHERE status != 'COMPLETED';"
    );
    const count = res[0]?.count ?? 0;
    db.runSync("DELETE FROM outbox_mutations WHERE status != 'COMPLETED';");
    return count;
  } catch (err) {
    console.warn('[outboxProcessor] Error clearing failed mutations:', err);
    return 0;
  }
}

/**
 * Status result returned by processOutboxQueue.
 */
export interface ProcessOutboxResult {
  processedCount: number;
  successCount: number;
  failedCount: number;
  abortedDueToOffline: boolean;
  errors: string[];
}

// Global flag to prevent concurrent outbox flushes
let isProcessingOutbox = false;

/**
 * Formats a clean human-readable error from an AxiosError or generic error.
 */
function extractDetailedErrorMessage(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const axiosError = err as AxiosError<{ errors?: Record<string, string[]>; detail?: string; title?: string }>;
    const data = axiosError.response?.data;
    if (data?.errors && typeof data.errors === 'object') {
      const fieldErrors = Object.entries(data.errors)
        .map(([field, msgs]) => `${field}: ${Array.isArray(msgs) ? msgs.join(', ') : msgs}`)
        .join('; ');
      return fieldErrors || data.title || axiosError.message;
    }
    if (data?.detail) {
      return data.detail;
    }
    if (data?.title) {
      return data.title;
    }
    if (axiosError.response?.status) {
      return `HTTP ${axiosError.response.status}: ${axiosError.message}`;
    }
    return axiosError.message;
  }
  return err instanceof Error ? err.message : String(err);
}

/**
 * Sequentially drains all pending mutations from the outbox queue.
 * Transmits each mutation with an HTTP Idempotency-Key header.
 * 
 * If an error occurs (network drop OR client validation failure), processing halts
 * immediately so subsequent dependent operations remain strictly in order.
 */
export async function processOutboxQueue(): Promise<ProcessOutboxResult> {
  if (isProcessingOutbox) {
    return {
      processedCount: 0,
      successCount: 0,
      failedCount: 0,
      abortedDueToOffline: false,
      errors: ['Outbox is already processing in another cycle.'],
    };
  }

  isProcessingOutbox = true;
  const result: ProcessOutboxResult = {
    processedCount: 0,
    successCount: 0,
    failedCount: 0,
    abortedDueToOffline: false,
    errors: [],
  };

  try {
    const pending = getPendingMutations();
    if (pending.length === 0) {
      return result;
    }

    const db = Platform.OS !== 'web' ? getDatabase() : null;

    for (const mutation of pending) {
      result.processedCount++;

      // 1. Mark mutation as SYNCING
      if (Platform.OS === 'web') {
        mutation.status = 'SYNCING';
      } else if (db) {
        db.runSync(
          "UPDATE outbox_mutations SET status = 'SYNCING' WHERE id = ?;",
          [mutation.id]
        );
      }

      try {
        const parsedPayload = JSON.parse(mutation.payload_json);

        // 2. Dispatch request via centralized Axios client with Idempotency-Key
        await apiClient.request({
          url: mutation.endpoint,
          method: mutation.http_method,
          data: parsedPayload,
          headers: {
            'Idempotency-Key': mutation.idempotency_key,
          },
        });

        // 3. Mark mutation as COMPLETED
        const completedAt = new Date().toISOString();
        if (Platform.OS === 'web') {
          mutation.status = 'COMPLETED';
          mutation.synced_at = completedAt;
        } else if (db) {
          db.runSync(
            "UPDATE outbox_mutations SET status = 'COMPLETED', synced_at = ? WHERE id = ?;",
            [completedAt, mutation.id]
          );

          // 4. Update corresponding local SQLite operational tables based on operation type
          if (mutation.operation_type === 'ATTENDANCE_DRAFT') {
            const farmId = parsedPayload.farmId as string;
            const attDate = parsedPayload.attendanceDate as string;
            if (farmId && attDate) {
              db.runSync(
                `UPDATE local_labor_attendances 
                 SET sync_status = 'SYNCED' 
                 WHERE farm_id = ? AND attendance_date = ?;`,
                [farmId, attDate]
              );
            }
          } else if (mutation.operation_type === 'ATTENDANCE_FINALIZE') {
            const farmId = parsedPayload.farmId as string;
            const attDate = parsedPayload.attendanceDate as string;
            if (farmId && attDate) {
              db.runSync(
                `UPDATE local_labor_attendances 
                 SET status = 2, sync_status = 'SYNCED', finalized_at = ?
                 WHERE farm_id = ? AND attendance_date = ?;`,
                [completedAt, farmId, attDate]
              );
            }
          } else if (mutation.operation_type === 'WORKER_CREATE') {
            const workerId = parsedPayload.id as string;
            if (workerId) {
              db.runSync(
                "UPDATE cached_workers SET is_locally_created = 0 WHERE id = ?;",
                [workerId]
              );
            }
          }
        }

        result.successCount++;
      } catch (err: unknown) {
        result.failedCount++;
        const errorMessage = extractDetailedErrorMessage(err);
        result.errors.push(`[${mutation.operation_type}] ${errorMessage}`);

        // Check if error is network / offline related
        const isNetworkError =
          errorMessage.toLowerCase().includes('network') ||
          errorMessage.toLowerCase().includes('offline') ||
          errorMessage.toLowerCase().includes('timeout') ||
          errorMessage.toLowerCase().includes('simulated offline');

        if (isNetworkError) {
          // Revert back to PENDING so next online flush picks it up
          if (Platform.OS === 'web') {
            mutation.status = 'PENDING';
          } else if (db) {
            db.runSync(
              "UPDATE outbox_mutations SET status = 'PENDING', last_error = ? WHERE id = ?;",
              [errorMessage, mutation.id]
            );
          }
          result.abortedDueToOffline = true;
          // Halt queue on network drop to preserve sequential ordering!
          break;
        } else {
          // Server validation / business error: increment retry count
          const newRetryCount = mutation.retry_count + 1;
          const newStatus = newRetryCount >= 5 ? 'FAILED' : 'PENDING';

          if (Platform.OS === 'web') {
            mutation.status = newStatus;
            mutation.retry_count = newRetryCount;
            mutation.last_error = errorMessage;
          } else if (db) {
            db.runSync(
              `UPDATE outbox_mutations 
               SET status = ?, retry_count = ?, last_error = ? 
               WHERE id = ?;`,
              [newStatus, newRetryCount, errorMessage, mutation.id]
            );
          }

          // IMPORTANT: If this mutation failed, any subsequent dependent mutation in the queue
          // (such as assigning the worker or drafting their attendance) CANNOT succeed.
          // Halt processing so dependent mutations do not cascade into confusing 404/400 errors!
          break;
        }
      }
    }
  } finally {
    isProcessingOutbox = false;
  }

  return result;
}
