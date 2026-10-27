import { Platform } from 'react-native';
import { getDatabase } from '../db/database';
import { TokenStorage } from '../auth/tokenStorage';
import { apiClient } from '../api/client';
import { enqueueMutation, generateUuid, processOutboxQueue } from '../sync/outboxProcessor';

/**
 * Supported attendance types mirroring FarmManagement.Domain.Enums.AttendanceType.
 */
export enum AttendanceType {
  FullDay = 1,
  HalfDay = 2,
  Hourly = 3,
  NotWorked = 4,
}

/**
 * Attendance status lifecycle states mirroring FarmManagement.Domain.Enums.AttendanceStatus.
 */
export enum AttendanceStatus {
  Draft = 1,
  Finalized = 2,
}

export type AttendanceTypeString = 'FullDay' | 'HalfDay' | 'Hourly' | 'NotWorked';

/**
 * Converts numeric AttendanceType enum to server DTO string.
 */
export function attendanceTypeToString(type: AttendanceType): AttendanceTypeString {
  switch (type) {
    case AttendanceType.FullDay:
      return 'FullDay';
    case AttendanceType.HalfDay:
      return 'HalfDay';
    case AttendanceType.Hourly:
      return 'Hourly';
    case AttendanceType.NotWorked:
      return 'NotWorked';
    default:
      return 'FullDay';
  }
}

/**
 * Converts server attendance type string to numeric enum.
 */
export function stringToAttendanceType(str?: string | null): AttendanceType {
  if (!str) return AttendanceType.FullDay;
  switch (str.toLowerCase()) {
    case 'fullday':
    case 'full_day':
      return AttendanceType.FullDay;
    case 'halfday':
    case 'half_day':
      return AttendanceType.HalfDay;
    case 'hourly':
      return AttendanceType.Hourly;
    case 'notworked':
    case 'not_worked':
      return AttendanceType.NotWorked;
    default:
      return AttendanceType.FullDay;
  }
}

/**
 * Returns default working hours for the selected attendance type:
 * FullDay -> 8.0h, HalfDay -> 4.0h, Hourly -> 1.0h, NotWorked -> 0.0h.
 * Note: Hourly defaults to 1.0h minimum to ensure amount > 0 and satisfy backend validation.
 */
export function getDefaultHoursForType(type: AttendanceType): number {
  switch (type) {
    case AttendanceType.FullDay:
      return 8.0;
    case AttendanceType.HalfDay:
      return 4.0;
    case AttendanceType.Hourly:
      return 1.0;
    case AttendanceType.NotWorked:
      return 0.0;
  }
}

/**
 * Wage rate record from SQLite cached_labor_wage_rates.
 */
export interface RawWageRate {
  gender: number;
  wage_type: number;
  wage_rate: number;
  currency_id: string;
  effective_from: string;
  effective_to?: string | null;
  is_active: number;
}

/**
 * Represents a worker line item within the attendance roster.
 */
export interface RosterWorkerItem {
  workerId: string;
  firstName: string;
  lastName?: string | null;
  displayName: string;
  gender: number; // 1=Male, 2=Female, 3=Other
  laborCategoryId?: string | null;
  attendanceType: AttendanceType;
  workingHours: number;
  calculatedRate: number;
  calculatedAmount: number;
  currencyId?: string | null;
  notes?: string | null;
  isCheckedIn: boolean;
  isLocallyCreated: boolean;
  localAttendanceId?: string | null;
  status: AttendanceStatus;
  syncStatus: 'LOCAL_DRAFT' | 'SYNCED' | 'NOT_SAVED';
}

/**
 * Daily roster summary statistics for UI telemetry strip and state gating.
 */
export interface DailyRosterSummary {
  totalAssigned: number;
  presentCount: number;
  absentCount: number;
  estimatedEarnings: number;
  isFinalized: boolean;
  isLockedOnline: boolean;
  hasLocalDraft: boolean;
}

/**
 * In-memory fallback attendance cache for web browser preview.
 */
interface WebAttendanceRow {
  id: string;
  organization_id: string;
  farm_id: string;
  worker_id: string;
  attendance_date: string;
  attendance_type: number;
  working_hours: number | null;
  calculated_rate: number | null;
  calculated_amount: number | null;
  currency_id: string | null;
  status: number;
  notes: string | null;
  sync_status: string;
  created_at: string;
  created_by: string;
}
const webAttendanceStore: WebAttendanceRow[] = [];

/**
 * Computes estimated wage for a given worker based on effective wage rate rules:
 * - Matches worker gender.
 * - Filters by effective date: effective_from <= attendanceDate && (effective_to == null || effective_to >= attendanceDate).
 * - Matches exact wage_type: FullDay = 1, HalfDay = 2, Hourly = 3.
 * - For NotWorked: amount = 0.
 */
export function calculateEstimatedWage(
  gender: number,
  type: AttendanceType,
  hours: number,
  wageRates: RawWageRate[],
  attendanceDate?: string
): { rate: number; amount: number; currencyId: string | null } {
  if (type === AttendanceType.NotWorked) {
    return { rate: 0, amount: 0, currencyId: null };
  }

  const targetDate = attendanceDate || new Date().toISOString().split('T')[0];

  // 1. Filter active wage rates matching target date
  const activeRates = wageRates.filter((w) => {
    if (w.is_active !== 1) return false;
    if (w.effective_from && w.effective_from > targetDate) return false;
    if (w.effective_to && w.effective_to < targetDate) return false;
    return true;
  });

  // 2. Filter rates matching worker gender (with fallback to all active rates if gender-specific is absent)
  const genderRates = activeRates.filter((w) => w.gender === gender);
  const eligibleRates = genderRates.length > 0 ? genderRates : activeRates;

  if (eligibleRates.length === 0) {
    return { rate: 0, amount: 0, currencyId: null };
  }

  // 3. Find type-specific rate or compute proportionally
  const fullDayMatch = eligibleRates.find((w) => w.wage_type === 1);
  const halfDayMatch = eligibleRates.find((w) => w.wage_type === 2);
  const hourlyMatch = eligibleRates.find((w) => w.wage_type === 3);

  const fallbackCurrencyId = eligibleRates[0]?.currency_id || null;

  if (type === AttendanceType.FullDay) {
    if (fullDayMatch) {
      const rate = Number(fullDayMatch.wage_rate);
      return { rate, amount: rate, currencyId: fullDayMatch.currency_id };
    }
    return { rate: 0, amount: 0, currencyId: fallbackCurrencyId };
  }

  if (type === AttendanceType.HalfDay) {
    if (halfDayMatch) {
      const rate = Number(halfDayMatch.wage_rate);
      return { rate, amount: rate, currencyId: halfDayMatch.currency_id };
    }
    if (fullDayMatch) {
      const fullRate = Number(fullDayMatch.wage_rate);
      const halfRate = Math.round((fullRate / 2) * 100) / 100;
      return { rate: halfRate, amount: halfRate, currencyId: fullDayMatch.currency_id };
    }
    return { rate: 0, amount: 0, currencyId: fallbackCurrencyId };
  }

  if (type === AttendanceType.Hourly) {
    if (hourlyMatch) {
      const hourlyRate = Number(hourlyMatch.wage_rate);
      const amount = Math.round(hourlyRate * hours * 100) / 100;
      return { rate: hourlyRate, amount, currencyId: hourlyMatch.currency_id };
    }
    if (fullDayMatch) {
      const fullRate = Number(fullDayMatch.wage_rate);
      const derivedHourlyRate = Math.round((fullRate / 8) * 100) / 100;
      const amount = Math.round(derivedHourlyRate * hours * 100) / 100;
      return { rate: derivedHourlyRate, amount, currencyId: fullDayMatch.currency_id };
    }
    if (halfDayMatch) {
      const halfRate = Number(halfDayMatch.wage_rate);
      const derivedHourlyRate = Math.round((halfRate / 4) * 100) / 100;
      const amount = Math.round(derivedHourlyRate * hours * 100) / 100;
      return { rate: derivedHourlyRate, amount, currencyId: halfDayMatch.currency_id };
    }
    return { rate: 0, amount: 0, currencyId: fallbackCurrencyId };
  }

  return { rate: 0, amount: 0, currencyId: fallbackCurrencyId };
}

/**
 * Server record contract returned by GET /api/attendance/daily.
 */
export interface ServerAttendanceRecordResponse {
  id: string;
  organizationId: string;
  farmId: string;
  farmName: string;
  workerId: string;
  workerDisplayName: string;
  workerFirstName: string;
  workerLastName?: string | null;
  gender: string;
  laborCategoryId?: string | null;
  laborCategoryName?: string | null;
  employmentType: string;
  attendanceDate: string;
  attendanceType: string;
  workingHours?: number | null;
  calculatedRate?: number | null;
  calculatedAmount?: number | null;
  currencyId?: string | null;
  currencyCode?: string | null;
  currencySymbol?: string | null;
  status: string;
  notes?: string | null;
  finalizedAt?: string | null;
  finalizedBy?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Daily attendance response payload envelope from GET /api/attendance/daily.
 */
export interface ServerDailyAttendanceResponse {
  farmId: string;
  farmName: string;
  attendanceDate: string;
  summary: {
    totalCount: number;
    workedCount: number;
    fullDayCount: number;
    halfDayCount: number;
    hourlyCount: number;
    notWorkedCount: number;
    estimatedEarnings: number;
    status: string;
  };
  records: ServerAttendanceRecordResponse[];
}

/**
 * Normalizes backend gender string to numeric code: 1=Male, 2=Female, 3=Other.
 */
function parseGender(genderStr?: string | null): number {
  if (!genderStr) return 1;
  const lower = genderStr.toLowerCase();
  if (lower.startsWith('f')) return 2;
  if (lower.startsWith('m')) return 1;
  return 3;
}

/**
 * Fetches latest daily attendance records from backend API and synchronizes them
 * atomically into local SQLite storage with sync_status = 'SYNCED'.
 * Ensures zero data loss, exact rate/earning parity, and triggers proper screen lock.
 */
export async function syncDailyAttendanceFromServer(
  farmId: string,
  attendanceDate: string
): Promise<{ success: boolean; recordCount: number; error?: string }> {
  try {
    const response = await apiClient.get<ServerDailyAttendanceResponse>(
      '/api/attendance/daily',
      {
        params: {
          farmId,
          date: attendanceDate,
        },
      }
    );

    const data = response.data;
    const serverRecords = data?.records || [];
    const now = new Date().toISOString();

    if (Platform.OS === 'web') {
      // In web preview sandbox, clear previously synced records for this farm and date
      for (let i = webAttendanceStore.length - 1; i >= 0; i--) {
        const r = webAttendanceStore[i];
        if (r.farm_id === farmId && r.attendance_date === attendanceDate && r.sync_status === 'SYNCED') {
          webAttendanceStore.splice(i, 1);
        }
      }

      for (const rec of serverRecords) {
        const attType = stringToAttendanceType(rec.attendanceType);
        const status = rec.status?.toUpperCase() === 'FINALIZED' ? AttendanceStatus.Finalized : AttendanceStatus.Draft;
        webAttendanceStore.push({
          id: rec.id,
          organization_id: rec.organizationId,
          farm_id: rec.farmId,
          worker_id: rec.workerId,
          attendance_date: rec.attendanceDate,
          attendance_type: attType,
          working_hours: rec.workingHours ?? (attType === AttendanceType.Hourly ? 1.0 : getDefaultHoursForType(attType)),
          calculated_rate: rec.calculatedRate ?? null,
          calculated_amount: rec.calculatedAmount ?? null,
          currency_id: rec.currencyId ?? null,
          status,
          notes: rec.notes ?? null,
          sync_status: 'SYNCED',
          created_at: rec.createdAt || now,
          created_by: rec.createdBy || '',
        });
      }
    } else {
      const db = getDatabase();
      if (!db) {
        return { success: false, recordCount: 0, error: 'Database is not initialized.' };
      }

      db.withTransactionSync(() => {
        // Purge old synced records for this farm and date to reflect latest server state
        db.runSync(
          `DELETE FROM local_labor_attendances 
           WHERE farm_id = ? AND attendance_date = ? AND sync_status = 'SYNCED';`,
          [farmId, attendanceDate]
        );

        for (const rec of serverRecords) {
          const attType = stringToAttendanceType(rec.attendanceType);
          const status = rec.status?.toUpperCase() === 'FINALIZED' ? AttendanceStatus.Finalized : AttendanceStatus.Draft;
          const genderNum = parseGender(rec.gender);

          // 1. Ensure worker exists in cached_workers so roster display includes them
          db.runSync(
            `INSERT OR IGNORE INTO cached_workers (
              id, organization_id, first_name, last_name, display_name,
              gender, employment_type, labor_category_id, is_active, is_locally_created
            ) VALUES (?, ?, ?, ?, ?, ?, 1, ?, 1, 0);`,
            [
              rec.workerId,
              rec.organizationId,
              rec.workerFirstName || '',
              rec.workerLastName || null,
              rec.workerDisplayName || `${rec.workerFirstName} ${rec.workerLastName || ''}`.trim(),
              genderNum,
              rec.laborCategoryId || null,
            ]
          );

          // 2. Ensure assignment exists in cached_worker_farm_assignments
          const assignmentId = generateUuid();
          db.runSync(
            `INSERT OR IGNORE INTO cached_worker_farm_assignments (
              id, organization_id, worker_id, farm_id, assigned_from, is_active
            ) VALUES (?, ?, ?, ?, ?, 1);`,
            [assignmentId, rec.organizationId, rec.workerId, farmId, attendanceDate]
          );

          // 3. Upsert into local_labor_attendances with sync_status = 'SYNCED'
          db.runSync(
            `INSERT INTO local_labor_attendances (
              id, organization_id, farm_id, worker_id, attendance_date,
              attendance_type, working_hours, calculated_rate, calculated_amount,
              currency_id, status, notes, finalized_at, finalized_by,
              created_at, created_by, updated_at, updated_by, sync_status
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'SYNCED')
            ON CONFLICT(id) DO UPDATE SET
              attendance_type = excluded.attendance_type,
              working_hours = excluded.working_hours,
              calculated_rate = excluded.calculated_rate,
              calculated_amount = excluded.calculated_amount,
              currency_id = excluded.currency_id,
              status = excluded.status,
              notes = excluded.notes,
              finalized_at = excluded.finalized_at,
              finalized_by = excluded.finalized_by,
              updated_at = excluded.updated_at,
              updated_by = excluded.updated_by,
              sync_status = 'SYNCED';`,
            [
              rec.id,
              rec.organizationId,
              farmId,
              rec.workerId,
              attendanceDate,
              attType,
              rec.workingHours ?? null,
              rec.calculatedRate ?? null,
              rec.calculatedAmount ?? null,
              rec.currencyId ?? null,
              status,
              rec.notes ?? null,
              rec.finalizedAt ?? null,
              rec.finalizedBy ?? null,
              rec.createdAt || now,
              rec.createdBy || '',
              rec.updatedAt ?? null,
              rec.updatedBy ?? null,
            ]
          );
        }
      });
    }

    return { success: true, recordCount: serverRecords.length };
  } catch (err: unknown) {
    console.warn('[attendanceService] syncDailyAttendanceFromServer failed (offline fallback):', err);
    const msg = err instanceof Error ? err.message : String(err);
    return { success: false, recordCount: 0, error: msg };
  }
}

/**
 * Loads the complete daily attendance roster for a specific farm and date.
 * Merges cached farm workers, active wage rates, and any locally saved draft records.
 */
export async function loadDailyRoster(
  farmId: string,
  attendanceDate: string
): Promise<{ items: RosterWorkerItem[]; summary: DailyRosterSummary; wageRates: RawWageRate[] }> {
  const db = Platform.OS !== 'web' ? getDatabase() : null;

  // 1. Fetch eligible workers for this farm
  let workers: Array<{
    id: string;
    first_name: string;
    last_name?: string | null;
    display_name: string;
    gender: number;
    labor_category_id?: string | null;
    is_locally_created: number;
  }> = [];

  if (Platform.OS === 'web') {
    workers = [
      { id: 'w1', first_name: 'Ashok', last_name: 'Kokatnoor', display_name: 'Ashok Kokatnoor', gender: 1, is_locally_created: 0 },
      { id: 'w2', first_name: 'Ravi', last_name: 'Kokatnoor', display_name: 'Ravi Kokatnoor', gender: 1, is_locally_created: 0 },
      { id: 'w3', first_name: 'Sharavoa', last_name: null, display_name: 'Sharavoa', gender: 2, is_locally_created: 0 },
    ];
  } else if (db) {
    try {
      workers = db.getAllSync(
        `SELECT DISTINCT w.id, w.first_name, w.last_name, w.display_name, w.gender,
                         w.labor_category_id, w.is_locally_created
         FROM cached_workers w
         LEFT JOIN cached_worker_farm_assignments a ON a.worker_id = w.id
         WHERE (a.farm_id = ? OR w.is_locally_created = 1) AND w.is_active = 1
         ORDER BY w.display_name ASC;`,
        [farmId]
      );
    } catch (err) {
      console.warn('[attendanceService] Error querying farm workers:', err);
      workers = [];
    }
  }

  // 2. Fetch wage rates for wage calculation preview
  let wageRates: RawWageRate[] = [];
  if (Platform.OS === 'web') {
    wageRates = [
      { gender: 1, wage_type: 1, wage_rate: 550, currency_id: 'c1', effective_from: '2026-09-12', effective_to: null, is_active: 1 },
      { gender: 1, wage_type: 2, wage_rate: 350, currency_id: 'c1', effective_from: '2026-09-01', effective_to: null, is_active: 1 },
      { gender: 2, wage_type: 1, wage_rate: 450, currency_id: 'c1', effective_from: '2026-09-01', effective_to: null, is_active: 1 },
    ];
  } else if (db) {
    try {
      wageRates = db.getAllSync<RawWageRate>(
        `SELECT gender, wage_type, wage_rate, currency_id, effective_from, effective_to, is_active 
         FROM cached_labor_wage_rates 
         WHERE is_active = 1;`
      );
    } catch (err) {
      console.warn('[attendanceService] Error querying wage rates:', err);
    }
  }

  // 3. Fetch existing local attendance records for this farm and date
  const existingLocalAttendanceMap = new Map<string, {
    id: string;
    attendance_type: number;
    working_hours: number | null;
    calculated_rate: number | null;
    calculated_amount: number | null;
    currency_id: string | null;
    notes: string | null;
    status: number;
    sync_status: string;
  }>();

  if (Platform.OS === 'web') {
    const existing = webAttendanceStore.filter((r) => r.farm_id === farmId && r.attendance_date === attendanceDate);
    for (const r of existing) {
      existingLocalAttendanceMap.set(r.worker_id, {
        id: r.id,
        attendance_type: r.attendance_type,
        working_hours: r.working_hours,
        calculated_rate: r.calculated_rate,
        calculated_amount: r.calculated_amount,
        currency_id: r.currency_id,
        notes: r.notes,
        status: r.status,
        sync_status: r.sync_status,
      });
    }
  } else if (db) {
    try {
      const records = db.getAllSync<{
        id: string;
        worker_id: string;
        attendance_type: number;
        working_hours: number | null;
        calculated_rate: number | null;
        calculated_amount: number | null;
        currency_id: string | null;
        notes: string | null;
        status: number;
        sync_status: string;
      }>(
        `SELECT id, worker_id, attendance_type, working_hours, calculated_rate, calculated_amount, currency_id, notes, status, sync_status
         FROM local_labor_attendances
         WHERE farm_id = ? AND attendance_date = ?;`,
        [farmId, attendanceDate]
      );
      for (const r of records) {
        existingLocalAttendanceMap.set(r.worker_id, r);
      }
    } catch (err) {
      console.warn('[attendanceService] Error querying local attendances:', err);
    }
  }

  // 4. Build merged roster items
  let isRosterFinalized = false;
  const items: RosterWorkerItem[] = workers.map((worker) => {
    const existing = existingLocalAttendanceMap.get(worker.id);

    // If an attendance row exists in SQLite, use its type; otherwise default to FullDay in Exception Muster
    const attendanceType: AttendanceType = existing
      ? (existing.attendance_type as AttendanceType)
      : AttendanceType.FullDay;

    const workingHours = existing && existing.working_hours !== null
      ? Number(existing.working_hours)
      : getDefaultHoursForType(attendanceType);

    const wage = calculateEstimatedWage(worker.gender, attendanceType, workingHours, wageRates, attendanceDate);

    const calculatedRate = (existing && existing.calculated_rate !== null && existing.calculated_rate !== undefined)
      ? Number(existing.calculated_rate)
      : wage.rate;

    const calculatedAmount = (existing && existing.calculated_amount !== null && existing.calculated_amount !== undefined)
      ? Number(existing.calculated_amount)
      : wage.amount;

    const currencyId = existing?.currency_id || wage.currencyId;

    const status: AttendanceStatus = existing
      ? (existing.status as AttendanceStatus)
      : AttendanceStatus.Draft;

    if (status === AttendanceStatus.Finalized) {
      isRosterFinalized = true;
    }

    const syncStatus: RosterWorkerItem['syncStatus'] = existing
      ? (existing.sync_status as 'LOCAL_DRAFT' | 'SYNCED')
      : 'NOT_SAVED';

    // A worker is checked in if an existing non-absent record exists
    const isCheckedIn = Boolean(existing && existing.attendance_type !== AttendanceType.NotWorked);

    return {
      workerId: worker.id,
      firstName: worker.first_name,
      lastName: worker.last_name,
      displayName: worker.display_name,
      gender: worker.gender,
      laborCategoryId: worker.labor_category_id,
      attendanceType,
      workingHours,
      calculatedRate,
      calculatedAmount,
      currencyId,
      notes: existing?.notes ?? null,
      isCheckedIn,
      isLocallyCreated: Boolean(worker.is_locally_created),
      localAttendanceId: existing?.id ?? null,
      status,
      syncStatus,
    };
  });

  // Ensure any workers present in local_labor_attendances who were not in workers list are included
  const knownWorkerIds = new Set(workers.map((w) => w.id));
  for (const [workerId, existing] of existingLocalAttendanceMap.entries()) {
    if (!knownWorkerIds.has(workerId)) {
      let extraWorker: { first_name: string; last_name?: string | null; display_name: string; gender: number; labor_category_id?: string | null } | undefined;
      if (db) {
        try {
          const res = db.getAllSync<{ first_name: string; last_name?: string | null; display_name: string; gender: number; labor_category_id?: string | null }>(
            'SELECT first_name, last_name, display_name, gender, labor_category_id FROM cached_workers WHERE id = ?;',
            [workerId]
          );
          if (res.length > 0) extraWorker = res[0];
        } catch {
          // ignore
        }
      }

      const attType = existing.attendance_type as AttendanceType;
      const hours = existing.working_hours !== null ? Number(existing.working_hours) : getDefaultHoursForType(attType);
      const isCheckedIn = existing.attendance_type !== AttendanceType.NotWorked;
      const st = existing.status as AttendanceStatus;
      if (st === AttendanceStatus.Finalized) {
        isRosterFinalized = true;
      }

      items.push({
        workerId,
        firstName: extraWorker?.first_name || '',
        lastName: extraWorker?.last_name || null,
        displayName: extraWorker?.display_name || `Worker (${workerId.slice(0, 6)})`,
        gender: extraWorker?.gender || 1,
        laborCategoryId: extraWorker?.labor_category_id || null,
        attendanceType: attType,
        workingHours: hours,
        calculatedRate: existing.calculated_rate !== null ? Number(existing.calculated_rate) : 0,
        calculatedAmount: existing.calculated_amount !== null ? Number(existing.calculated_amount) : 0,
        currencyId: existing.currency_id,
        notes: existing.notes,
        isCheckedIn,
        isLocallyCreated: false,
        localAttendanceId: existing.id,
        status: st,
        syncStatus: (existing.sync_status as 'LOCAL_DRAFT' | 'SYNCED') || 'SYNCED',
      });
    }
  }

  // 5. Calculate summary metrics and screen gating flags
  const totalAssigned = items.length;
  let presentCount = 0;
  let absentCount = 0;
  let estimatedEarnings = 0;
  const hasLocalDraft = existingLocalAttendanceMap.size > 0;
  let isRosterLockedOnline = false;

  for (const item of items) {
    if (item.isCheckedIn) {
      presentCount++;
      estimatedEarnings += item.calculatedAmount;
    } else {
      absentCount++;
    }
    if (item.syncStatus === 'SYNCED') {
      isRosterLockedOnline = true;
    }
  }

  // Also check if any raw existing local record has sync_status === 'SYNCED'
  for (const rec of existingLocalAttendanceMap.values()) {
    if (rec.sync_status === 'SYNCED') {
      isRosterLockedOnline = true;
    }
    if (rec.status === AttendanceStatus.Finalized) {
      isRosterFinalized = true;
    }
  }

  return {
    items,
    summary: {
      totalAssigned,
      presentCount,
      absentCount,
      estimatedEarnings: Math.round(estimatedEarnings * 100) / 100,
      isFinalized: isRosterFinalized,
      isLockedOnline: isRosterLockedOnline && !isRosterFinalized,
      hasLocalDraft,
    },
    wageRates,
  };
}

/**
 * Saves marked attendance records exclusively to local SQLite database (Step 1).
 * Deletes any unmarked worker records for this farm and date from SQLite.
 * Does NOT transmit mutations over the network.
 */
export async function saveOfflineDraft(
  farmId: string,
  attendanceDate: string,
  items: RosterWorkerItem[]
): Promise<{ success: boolean; savedCount: number; error?: string }> {
  try {
    const user = await TokenStorage.getUser();
    const organizationId = user?.organizationId || '00000000-0000-0000-0000-000000000000';
    const userId = user?.id || '00000000-0000-0000-0000-000000000000';
    const now = new Date().toISOString();

    const attendingItems = items.filter((i) => i.isCheckedIn && i.attendanceType !== AttendanceType.NotWorked);
    const unselectedWorkerIds = items.filter((i) => !i.isCheckedIn || i.attendanceType === AttendanceType.NotWorked).map((i) => i.workerId);

    if (Platform.OS === 'web') {
      for (let i = webAttendanceStore.length - 1; i >= 0; i--) {
        const r = webAttendanceStore[i];
        if (r.farm_id === farmId && r.attendance_date === attendanceDate && unselectedWorkerIds.includes(r.worker_id)) {
          webAttendanceStore.splice(i, 1);
        }
      }

      for (const item of attendingItems) {
        const id = item.localAttendanceId || generateUuid();
        item.localAttendanceId = id;
        item.status = AttendanceStatus.Draft;
        item.syncStatus = 'LOCAL_DRAFT';

        const existingIdx = webAttendanceStore.findIndex((r) => r.farm_id === farmId && r.worker_id === item.workerId && r.attendance_date === attendanceDate);
        const row: WebAttendanceRow = {
          id,
          organization_id: organizationId,
          farm_id: farmId,
          worker_id: item.workerId,
          attendance_date: attendanceDate,
          attendance_type: item.attendanceType,
          working_hours: item.attendanceType === AttendanceType.Hourly ? item.workingHours : null,
          calculated_rate: item.calculatedRate,
          calculated_amount: item.calculatedAmount,
          currency_id: item.currencyId ?? null,
          status: AttendanceStatus.Draft,
          notes: item.notes ?? null,
          sync_status: 'LOCAL_DRAFT',
          created_at: now,
          created_by: userId,
        };

        if (existingIdx >= 0) {
          webAttendanceStore[existingIdx] = row;
        } else {
          webAttendanceStore.push(row);
        }
      }
    } else {
      const db = getDatabase();
      if (!db) throw new Error('Database is not initialized.');

      db.withTransactionSync(() => {
        if (unselectedWorkerIds.length > 0) {
          const placeholders = unselectedWorkerIds.map(() => '?').join(',');
          db.runSync(
            `DELETE FROM local_labor_attendances 
             WHERE farm_id = ? AND attendance_date = ? AND worker_id IN (${placeholders});`,
            [farmId, attendanceDate, ...unselectedWorkerIds]
          );
        }

        for (const item of attendingItems) {
          const id = item.localAttendanceId || generateUuid();
          item.localAttendanceId = id;
          item.status = AttendanceStatus.Draft;
          item.syncStatus = 'LOCAL_DRAFT';

          const workingHours = item.attendanceType === AttendanceType.Hourly ? item.workingHours : null;

          db.runSync(
            `INSERT INTO local_labor_attendances (
              id, organization_id, farm_id, worker_id, attendance_date,
              attendance_type, working_hours, calculated_rate, calculated_amount,
              currency_id, status, notes, created_at, created_by, sync_status
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'LOCAL_DRAFT')
            ON CONFLICT(id) DO UPDATE SET
              attendance_type = excluded.attendance_type,
              working_hours = excluded.working_hours,
              calculated_rate = excluded.calculated_rate,
              calculated_amount = excluded.calculated_amount,
              notes = excluded.notes,
              sync_status = 'LOCAL_DRAFT',
              updated_at = ?;`,
            [
              id,
              organizationId,
              farmId,
              item.workerId,
              attendanceDate,
              item.attendanceType,
              workingHours,
              item.calculatedRate,
              item.calculatedAmount,
              item.currencyId ?? null,
              AttendanceStatus.Draft,
              item.notes ?? null,
              now,
              userId,
              now,
            ]
          );
        }
      });
    }

    return { success: true, savedCount: attendingItems.length };
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err);
    console.error('[attendanceService] saveOfflineDraft error:', err);
    return { success: false, savedCount: 0, error: message };
  }
}

/**
 * Synchronizes the locally saved draft with the backend API (Step 2).
 * Formats the batch payload, sends it to the server via outbox queue or direct post,
 * and upon server confirmation, marks the local records as 'SYNCED'.
 */
export async function syncDraftOnline(
  farmId: string,
  attendanceDate: string,
  items: RosterWorkerItem[]
): Promise<{ success: boolean; syncedCount: number; error?: string }> {
  try {
    // 1. Ensure local SQLite records are completely updated first
    const offlineSave = await saveOfflineDraft(farmId, attendanceDate, items);
    if (!offlineSave.success) {
      return { success: false, syncedCount: 0, error: offlineSave.error || 'Failed to update local draft.' };
    }

    const attendingItems = items.filter((i) => i.isCheckedIn && i.attendanceType !== AttendanceType.NotWorked);
    const idempotencyKey = generateUuid();

    // 2. Prepare payload ensuring workingHours is null for Full/Half Day and valid number for Hourly
    const batchPayload = {
      farmId,
      attendanceDate,
      items: attendingItems.map((i) => ({
        id: i.localAttendanceId,
        workerId: i.workerId,
        attendanceType: attendanceTypeToString(i.attendanceType),
        workingHours: i.attendanceType === AttendanceType.Hourly ? i.workingHours : null,
        notes: i.notes || null,
      })),
      removeOmittedDrafts: true,
      idempotencyKey,
    };

    // 3. Enqueue mutation
    enqueueMutation({
      operationType: 'ATTENDANCE_DRAFT',
      endpoint: '/api/attendance/draft/batch',
      httpMethod: 'POST',
      idempotencyKey,
      payload: batchPayload,
    });

    // 4. Trigger synchronous outbox drain to immediately confirm sync with server
    const outboxResult = await processOutboxQueue();
    if (outboxResult.failedCount > 0 || outboxResult.abortedDueToOffline) {
      const errorMsg = outboxResult.errors.join('\n') || 'Server synchronization failed. Changes remain saved locally.';
      return { success: false, syncedCount: 0, error: errorMsg };
    }

    return { success: true, syncedCount: attendingItems.length };
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err);
    console.error('[attendanceService] syncDraftOnline error:', err);
    return { success: false, syncedCount: 0, error: message };
  }
}

/**
 * Saves draft attendance for a collection of workers on a farm and date.
 * (Preserved as a wrapper over syncDraftOnline for backwards compatibility).
 */
export async function saveDraftAttendance(
  farmId: string,
  attendanceDate: string,
  items: RosterWorkerItem[]
): Promise<{ success: boolean; idempotencyKey: string; error?: string }> {
  const result = await syncDraftOnline(farmId, attendanceDate, items);
  return { success: result.success, idempotencyKey: generateUuid(), error: result.error };
}

/**
 * Finalizes daily attendance for a farm and date.
 * Locks local records and enqueues POST /api/attendance/finalize.
 */
export async function finalizeAttendance(
  farmId: string,
  attendanceDate: string,
  attendanceIds?: string[]
): Promise<{ success: boolean; idempotencyKey: string; error?: string }> {
  try {
    const idempotencyKey = generateUuid();
    const now = new Date().toISOString();

    if (Platform.OS === 'web') {
      for (const r of webAttendanceStore) {
        if (r.farm_id === farmId && r.attendance_date === attendanceDate) {
          r.status = AttendanceStatus.Finalized;
          r.sync_status = 'LOCAL_DRAFT';
        }
      }
    } else {
      const db = getDatabase();
      if (!db) throw new Error('Database is not initialized.');

      db.runSync(
        `UPDATE local_labor_attendances 
         SET status = ?, finalized_at = ?, sync_status = 'LOCAL_DRAFT'
         WHERE farm_id = ? AND attendance_date = ?;`,
        [AttendanceStatus.Finalized, now, farmId, attendanceDate]
      );
    }

    const finalizePayload = {
      farmId,
      attendanceDate,
      attendanceIds: attendanceIds && attendanceIds.length > 0 ? attendanceIds : null,
      idempotencyKey,
    };

    enqueueMutation({
      operationType: 'ATTENDANCE_FINALIZE',
      endpoint: '/api/attendance/finalize',
      httpMethod: 'POST',
      idempotencyKey,
      payload: finalizePayload,
    });

    const outboxResult = await processOutboxQueue();
    if (outboxResult.failedCount > 0 || outboxResult.abortedDueToOffline) {
      const errorMsg = outboxResult.errors.join('\n') || 'Finalization failed on server.';
      return { success: false, idempotencyKey, error: errorMsg };
    }

    return { success: true, idempotencyKey };
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err);
    console.error('[attendanceService] finalizeAttendance error:', err);
    return { success: false, idempotencyKey: '', error: message };
  }
}

/**
 * Parameters for fast 30s quick-adding a worker in the field.
 */
export interface QuickAddWorkerParams {
  firstName: string;
  lastName?: string;
  gender: number; // 1=Male, 2=Female, 3=Other
  laborCategoryId?: string | null;
  joiningDate?: string;
  wageRates?: RawWageRate[];
}

/**
 * Quick-adds a worker directly in the field:
 * 1. Generates local client UUIDv4.
 * 2. Saves to `cached_workers` with `is_locally_created = 1`.
 * 3. Assigns worker to current farm in `cached_worker_farm_assignments`.
 * 4. Queues sequential outbox mutations: WORKER_CREATE (with employmentType: 'DAILY_WAGE') -> WORKER_ASSIGN.
 * 5. Returns a new `RosterWorkerItem` pre-selected as FullDay (8.0h) with correct wage.
 */
export async function quickAddWorker(
  farmId: string,
  params: QuickAddWorkerParams
): Promise<RosterWorkerItem> {
  const user = await TokenStorage.getUser();
  const organizationId = user?.organizationId || '00000000-0000-0000-0000-000000000000';
  const workerId = generateUuid();
  const assignmentId = generateUuid();
  const today = new Date().toISOString().split('T')[0];
  const effectiveJoiningDate = params.joiningDate || today;

  const displayName = `${params.firstName.trim()} ${params.lastName ? params.lastName.trim() : ''}`.trim();
  const genderStr = params.gender === 1 ? 'Male' : params.gender === 2 ? 'Female' : 'Other';

  if (Platform.OS !== 'web') {
    const db = getDatabase();
    if (db) {
      db.withTransactionSync(() => {
        db.runSync(
          `INSERT INTO cached_workers (
            id, organization_id, first_name, last_name, display_name,
            gender, employment_type, labor_category_id, is_active, is_locally_created
          ) VALUES (?, ?, ?, ?, ?, ?, 1, ?, 1, 1);`,
          [
            workerId,
            organizationId,
            params.firstName.trim(),
            params.lastName ? params.lastName.trim() : null,
            displayName,
            params.gender,
            params.laborCategoryId || null,
          ]
        );

        db.runSync(
          `INSERT INTO cached_worker_farm_assignments (
            id, organization_id, worker_id, farm_id, assigned_from, is_active
          ) VALUES (?, ?, ?, ?, ?, 1);`,
          [assignmentId, organizationId, workerId, farmId, effectiveJoiningDate]
        );
      });
    }
  }

  // 1. Enqueue Mutation 1: Worker Creation (CRITICAL: employmentType must be 'DAILY_WAGE')
  const workerMutation = enqueueMutation({
    operationType: 'WORKER_CREATE',
    endpoint: '/api/labor/workers',
    httpMethod: 'POST',
    payload: {
      id: workerId,
      firstName: params.firstName.trim(),
      lastName: params.lastName ? params.lastName.trim() : null,
      displayName,
      gender: genderStr,
      employmentType: 'DAILY_WAGE',
      laborCategoryId: params.laborCategoryId || null,
      joiningDate: effectiveJoiningDate,
    },
  });

  // 2. Enqueue Mutation 2: Farm Assignment
  enqueueMutation({
    operationType: 'WORKER_ASSIGN',
    endpoint: `/api/labor/workers/${workerId}/farms`,
    httpMethod: 'POST',
    dependencyMutationId: workerMutation.id,
    payload: {
      id: assignmentId,
      farmId,
      assignedFrom: effectiveJoiningDate,
    },
  });

  // 3. Compute accurate wage preview for the new worker
  const wage = calculateEstimatedWage(
    params.gender,
    AttendanceType.FullDay,
    8.0,
    params.wageRates || [],
    effectiveJoiningDate
  );

  return {
    workerId,
    firstName: params.firstName.trim(),
    lastName: params.lastName ? params.lastName.trim() : null,
    displayName,
    gender: params.gender,
    laborCategoryId: params.laborCategoryId || null,
    attendanceType: AttendanceType.FullDay,
    workingHours: 8.0,
    calculatedRate: wage.rate,
    calculatedAmount: wage.amount,
    currencyId: wage.currencyId,
    notes: null,
    isCheckedIn: true,
    isLocallyCreated: true,
    localAttendanceId: null,
    status: AttendanceStatus.Draft,
    syncStatus: 'NOT_SAVED',
  };
}
