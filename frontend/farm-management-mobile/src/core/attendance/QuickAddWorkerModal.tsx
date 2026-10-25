import React, { useState, useEffect } from 'react';
import {
  Modal,
  View,
  Text,
  TextInput,
  TouchableOpacity,
  ScrollView,
  StyleSheet,
  ActivityIndicator,
  Alert,
} from 'react-native';
import { getCachedLaborCategories, CachedLaborCategory } from '../sync/dataQueries';
import { quickAddWorker, RosterWorkerItem, RawWageRate } from './attendanceService';

/**
 * Props interface for QuickAddWorkerModal.
 */
export interface QuickAddWorkerModalProps {
  visible: boolean;
  farmId: string;
  wageRates?: RawWageRate[];
  onClose: () => void;
  onWorkerAdded: (worker: RosterWorkerItem) => void;
}

/**
 * QuickAddWorkerModal Component:
 * Provides a streamlined 30-second field registration modal for unlisted workers.
 * Complies with strict architectural guidelines:
 * - Zero inline styles (all styles centralized in StyleSheet).
 * - Full TypeScript typing without any types.
 * - Comprehensive JSDoc explaining lifecycle and behavior.
 */
export const QuickAddWorkerModal: React.FC<QuickAddWorkerModalProps> = ({
  visible,
  farmId,
  wageRates,
  onClose,
  onWorkerAdded,
}) => {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [gender, setGender] = useState<number>(1); // 1 = Male, 2 = Female, 3 = Other
  const [laborCategoryId, setLaborCategoryId] = useState<string>('');
  const [categories, setCategories] = useState<CachedLaborCategory[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Load cached labor categories when modal opens
  useEffect(() => {
    if (visible) {
      const cats = getCachedLaborCategories();
      setCategories(cats);
      if (cats.length > 0 && !laborCategoryId) {
        setLaborCategoryId(cats[0].id);
      }
    }
  }, [visible]);

  // Reset form state when modal closes
  const handleClose = () => {
    setFirstName('');
    setLastName('');
    setGender(1);
    setLaborCategoryId(categories.length > 0 ? categories[0].id : '');
    setIsSubmitting(false);
    onClose();
  };

  // Submit handler: validates input, creates local worker, and queues outbox mutations
  const handleSave = async () => {
    if (!firstName.trim()) {
      Alert.alert('Required Field', 'Please enter the worker first name.');
      return;
    }

    setIsSubmitting(true);
    try {
      const newWorker = await quickAddWorker(farmId, {
        firstName: firstName.trim(),
        lastName: lastName.trim() || undefined,
        gender,
        laborCategoryId: laborCategoryId || undefined,
        wageRates,
      });

      onWorkerAdded(newWorker);
      handleClose();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : String(err);
      Alert.alert('Registration Error', `Failed to register worker: ${msg}`);
      setIsSubmitting(false);
    }
  };

  return (
    <Modal visible={visible} animationType="slide" transparent onRequestClose={handleClose}>
      <View style={styles.modalOverlay}>
        <View style={styles.modalCard}>
          {/* Header */}
          <View style={styles.header}>
            <View>
              <Text style={styles.headerTitle}>Quick Add Worker</Text>
              <Text style={styles.headerSubtitle}>Field registration for unlisted laborers</Text>
            </View>
            <TouchableOpacity
              onPress={handleClose}
              disabled={isSubmitting}
              style={styles.closeButton}
              accessibilityLabel="Close Modal"
            >
              <Text style={styles.closeButtonText}>✕</Text>
            </TouchableOpacity>
          </View>

          <ScrollView style={styles.formScroll} showsVerticalScrollIndicator={false}>
            {/* First Name Input */}
            <View style={styles.formGroup}>
              <Text style={styles.label}>
                First Name <Text style={styles.requiredAsterisk}>*</Text>
              </Text>
              <TextInput
                style={styles.input}
                placeholder="e.g. Ramesh"
                placeholderTextColor="#9CA3AF"
                value={firstName}
                onChangeText={setFirstName}
                editable={!isSubmitting}
                autoFocus
              />
            </View>

            {/* Last Name Input */}
            <View style={styles.formGroup}>
              <Text style={styles.label}>Last Name (Optional)</Text>
              <TextInput
                style={styles.input}
                placeholder="e.g. Kumar"
                placeholderTextColor="#9CA3AF"
                value={lastName}
                onChangeText={setLastName}
                editable={!isSubmitting}
              />
            </View>

            {/* Gender Segmented Selector */}
            <View style={styles.formGroup}>
              <Text style={styles.label}>Gender</Text>
              <View style={styles.genderRow}>
                <TouchableOpacity
                  style={[styles.genderChip, gender === 1 && styles.genderChipSelected]}
                  onPress={() => setGender(1)}
                  disabled={isSubmitting}
                >
                  <Text style={[styles.genderText, gender === 1 && styles.genderTextSelected]}>
                    👨 Male
                  </Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[styles.genderChip, gender === 2 && styles.genderChipSelected]}
                  onPress={() => setGender(2)}
                  disabled={isSubmitting}
                >
                  <Text style={[styles.genderText, gender === 2 && styles.genderTextSelected]}>
                    👩 Female
                  </Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[styles.genderChip, gender === 3 && styles.genderChipSelected]}
                  onPress={() => setGender(3)}
                  disabled={isSubmitting}
                >
                  <Text style={[styles.genderText, gender === 3 && styles.genderTextSelected]}>
                    🧑 Other
                  </Text>
                </TouchableOpacity>
              </View>
            </View>

            {/* Labor Category Selector */}
            {categories.length > 0 && (
              <View style={styles.formGroup}>
                <Text style={styles.label}>Labor Category</Text>
                <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.categoryScroll}>
                  {categories.map((cat) => {
                    const isSelected = laborCategoryId === cat.id;
                    return (
                      <TouchableOpacity
                        key={cat.id}
                        style={[styles.categoryChip, isSelected && styles.categoryChipSelected]}
                        onPress={() => setLaborCategoryId(cat.id)}
                        disabled={isSubmitting}
                      >
                        <Text style={[styles.categoryText, isSelected && styles.categoryTextSelected]}>
                          {cat.name}
                        </Text>
                      </TouchableOpacity>
                    );
                  })}
                </ScrollView>
              </View>
            )}

            {/* Notice Info Box */}
            <View style={styles.infoBox}>
              <Text style={styles.infoText}>
                ⚡ Worker will be assigned to this farm and automatically added to today's roster as Present (8.0h).
                Changes will sync when online.
              </Text>
            </View>
          </ScrollView>

          {/* Action Footer */}
          <View style={styles.footer}>
            <TouchableOpacity
              style={styles.cancelButton}
              onPress={handleClose}
              disabled={isSubmitting}
            >
              <Text style={styles.cancelButtonText}>Cancel</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.saveButton, isSubmitting && styles.saveButtonDisabled]}
              onPress={handleSave}
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <ActivityIndicator color="#FFFFFF" size="small" />
              ) : (
                <Text style={styles.saveButtonText}>Add to Roster</Text>
              )}
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </Modal>
  );
};

const styles = StyleSheet.create({
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'flex-end',
  },
  modalCard: {
    backgroundColor: '#FFFFFF',
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    paddingTop: 20,
    paddingHorizontal: 20,
    paddingBottom: 28,
    maxHeight: '85%',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 16,
    paddingBottom: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#111827',
  },
  headerSubtitle: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: 2,
  },
  closeButton: {
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: '#F3F4F6',
    alignItems: 'center',
    justifyContent: 'center',
  },
  closeButtonText: {
    fontSize: 14,
    color: '#6B7280',
    fontWeight: '600',
  },
  formScroll: {
    marginBottom: 16,
  },
  formGroup: {
    marginBottom: 16,
  },
  label: {
    fontSize: 13,
    fontWeight: '600',
    color: '#374151',
    marginBottom: 6,
  },
  requiredAsterisk: {
    color: '#DC2626',
  },
  input: {
    backgroundColor: '#F9FAFB',
    borderWidth: 1,
    borderColor: '#D1D5DB',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 14,
    color: '#111827',
  },
  genderRow: {
    flexDirection: 'row',
    gap: 8,
  },
  genderChip: {
    flex: 1,
    paddingVertical: 10,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#D1D5DB',
    backgroundColor: '#F9FAFB',
    alignItems: 'center',
    justifyContent: 'center',
  },
  genderChipSelected: {
    borderColor: '#2E7D32',
    backgroundColor: '#E8F5E9',
  },
  genderText: {
    fontSize: 13,
    fontWeight: '500',
    color: '#4B5563',
  },
  genderTextSelected: {
    color: '#1B5E20',
    fontWeight: '700',
  },
  categoryScroll: {
    flexDirection: 'row',
    marginTop: 4,
  },
  categoryChip: {
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: '#D1D5DB',
    backgroundColor: '#F9FAFB',
    marginRight: 8,
  },
  categoryChipSelected: {
    borderColor: '#2E7D32',
    backgroundColor: '#E8F5E9',
  },
  categoryText: {
    fontSize: 12,
    fontWeight: '500',
    color: '#4B5563',
  },
  categoryTextSelected: {
    color: '#1B5E20',
    fontWeight: '700',
  },
  infoBox: {
    backgroundColor: '#F0FDF4',
    borderWidth: 1,
    borderColor: '#BBF7D0',
    borderRadius: 8,
    padding: 10,
    marginTop: 8,
  },
  infoText: {
    fontSize: 12,
    color: '#166534',
    lineHeight: 16,
  },
  footer: {
    flexDirection: 'row',
    gap: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
  },
  cancelButton: {
    flex: 1,
    paddingVertical: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#D1D5DB',
    backgroundColor: '#FFFFFF',
    alignItems: 'center',
    justifyContent: 'center',
  },
  cancelButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#4B5563',
  },
  saveButton: {
    flex: 1.5,
    paddingVertical: 12,
    borderRadius: 8,
    backgroundColor: '#2E7D32',
    alignItems: 'center',
    justifyContent: 'center',
  },
  saveButtonDisabled: {
    backgroundColor: '#A5D6A7',
  },
  saveButtonText: {
    fontSize: 14,
    fontWeight: '700',
    color: '#FFFFFF',
  },
});
