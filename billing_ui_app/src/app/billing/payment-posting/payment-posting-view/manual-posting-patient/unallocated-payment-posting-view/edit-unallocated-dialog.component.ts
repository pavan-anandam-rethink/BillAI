import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

export interface EditUnallocatedResult {
  patientId: number;
  unallocatedAmount: number;
  note?: string;
  rowVersion?: any;
  accountInfoId?: number;
  memberId?: number;
}

@Component({
  selector: 'edit-unallocated-dialog',
  templateUrl: './edit-unallocated-dialog.component.html',
  // optionally set styles or keep CSS file
})
export class EditUnallocatedDialogComponent implements OnInit {
  @Input() patientId!: number;
  @Input() patientName!: string;
  @Input() currentUnallocated!: number;
  @Input() note!: string;
  @Input() rowVersion?: string;
  @Input() accountInfoId?: number;
  @Input() memberId?: number;

  @Output() closeDialogEmitter = new EventEmitter<EditUnallocatedResult | null>();

  form: FormGroup;
  submitted = false;
  unallocatedAmount!: number;

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
       paymentAmount: [null, Validators.required],
      note: ['']
    });
  }

  ngOnInit(): void {
    this.form = this.fb.group({
      paymentAmount: [this.currentUnallocated || 0, [Validators.required, Validators.min(0.01)]],
      note: [this.note || '']
    });
  }

  onCancel() {
    this.closeDialogEmitter.emit(null);
  }

  // Save and emit payload
  onSave() {
    this.submitted = true;

    if (this.form.value.paymentAmount == null || this.form.value.paymentAmount== "") {
      return;
    }
     const unallocatedAmount = this.form.get('paymentAmount')?.value ?? 0;
    const note = this.form.get('note')?.value ?? '';

    // Emit the event payload
    this.closeDialogEmitter.emit({
      patientId: this.patientId,
      unallocatedAmount: Number(unallocatedAmount),
      note: note,
      rowVersion: this.rowVersion ?? null,
      accountInfoId: this.accountInfoId ?? null,
      memberId: this.memberId ?? null
    });
  }

  preventNegative(event: KeyboardEvent) {
    // Block minus key from keyboard
    if (event.key === '-' || event.key === 'Subtract') {
      event.preventDefault();
    }
  }

  blockNegativePaste(event: ClipboardEvent) {
    const pastedText = event.clipboardData?.getData('text');
    if (pastedText && pastedText.includes('-')) {
      event.preventDefault();
    }
  }

  limitDecimalPlaces(event: any) {
    const value = event.target.value;
    
    // If there’s a decimal, limit to 2 digits after it
    if (value.includes('.')) {
      const [integer, decimal] = value.split('.');
      if (decimal.length > 2) {
        event.target.value = `${integer}.${decimal.substring(0, 2)}`;
      }
    }
  }
}
