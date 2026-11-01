import { Component, effect, inject, input, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { describeHttpError } from '../../core/http-error';
import { Customer } from '../../core/models/customer';
import { CustomerService } from '../../core/services/customer.service';
import { Modal } from '../../shared/modal/modal';

/** Add / update customer modal. Pass `customer` to update, leave it null to add. */
@Component({
  selector: 'app-customer-form',
  imports: [ReactiveFormsModule, Modal],
  template: `
    <app-modal [open]="open()" [heading]="customer() ? 'Update customer' : 'Add customer'" (closed)="closed.emit()">
      <form id="customer-form" [formGroup]="form" (ngSubmit)="save()" novalidate>
        @if (error(); as message) {
          <p class="form-error" role="alert">{{ message }}</p>
        }
        <label class="field">
          <span class="field__label">Name</span>
          <input class="control" formControlName="name" autocomplete="organization" />
          @if (form.controls.name.touched && form.controls.name.invalid) {
            <span class="field__error">Enter a name.</span>
          }
        </label>
        <label class="field">
          <span class="field__label">Email</span>
          <input class="control" type="email" formControlName="email" autocomplete="email" />
          @if (form.controls.email.touched && form.controls.email.invalid) {
            <span class="field__error">Enter a valid email address.</span>
          }
        </label>
      </form>
      <div modal-footer>
        <button type="button" class="button button--ghost" (click)="closed.emit()">Cancel</button>
        <button type="submit" form="customer-form" class="button button--primary" [disabled]="saving()">
          {{ saving() ? 'Saving…' : customer() ? 'Save changes' : 'Add customer' }}
        </button>
      </div>
    </app-modal>
  `,
})
export class CustomerForm {
  private readonly customers = inject(CustomerService);
  private readonly fb = inject(NonNullableFormBuilder);

  readonly open = input(false);
  readonly customer = input<Customer | null>(null);
  readonly saved = output<Customer>();
  readonly closed = output<void>();

  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
  });

  constructor() {
    // Reset the form every time the modal opens.
    effect(() => {
      if (this.open()) {
        const customer = this.customer();
        this.form.reset({ name: customer?.name ?? '', email: customer?.email ?? '' });
        this.error.set(null);
      }
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.getRawValue();
    const existing = this.customer();
    this.saving.set(true);
    this.error.set(null);

    (existing ? this.customers.update(existing.id, request) : this.customers.create(request)).subscribe({
      next: (customer) => {
        this.saving.set(false);
        this.saved.emit(customer);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(describeHttpError(error));
      },
    });
  }
}
