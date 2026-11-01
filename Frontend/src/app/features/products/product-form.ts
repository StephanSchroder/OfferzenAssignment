import { Component, effect, inject, input, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CURRENCY_CODE } from '../../core/api.config';
import { describeHttpError } from '../../core/http-error';
import { Product } from '../../core/models/product';
import { ProductService } from '../../core/services/product.service';
import { Modal } from '../../shared/modal/modal';

/** Add / update product modal. Pass `product` to update, leave it null to add. */
@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule, Modal],
  template: `
    <app-modal [open]="open()" [heading]="product() ? 'Update product' : 'Add product'" (closed)="closed.emit()">
      <form id="product-form" [formGroup]="form" (ngSubmit)="save()" novalidate>
        @if (error(); as message) {
          <p class="form-error" role="alert">{{ message }}</p>
        }
        <label class="field">
          <span class="field__label">Name</span>
          <input class="control" formControlName="name" />
          @if (form.controls.name.touched && form.controls.name.invalid) {
            <span class="field__error">Enter a product name.</span>
          }
        </label>
        <label class="field">
          <span class="field__label">Description <span class="field__hint">(optional)</span></span>
          <textarea class="control" formControlName="description" rows="3"></textarea>
        </label>
        <label class="field">
          <span class="field__label">Price ({{ currency }})</span>
          <input class="control" type="number" inputmode="decimal" step="0.01" min="0" formControlName="price" />
          @if (form.controls.price.touched && form.controls.price.invalid) {
            <span class="field__error">Enter a price.</span>
          } @else {
            <span class="field__hint">Changing the price doesn't affect existing purchase orders.</span>
          }
        </label>
      </form>
      <div modal-footer>
        <button type="button" class="button button--ghost" (click)="closed.emit()">Cancel</button>
        <button type="submit" form="product-form" class="button button--primary" [disabled]="saving()">
          {{ saving() ? 'Saving…' : product() ? 'Save changes' : 'Add product' }}
        </button>
      </div>
    </app-modal>
  `,
})
export class ProductForm {
  private readonly products = inject(ProductService);
  private readonly fb = inject(NonNullableFormBuilder);

  readonly open = input(false);
  readonly product = input<Product | null>(null);
  readonly saved = output<Product>();
  readonly closed = output<void>();

  protected readonly currency = CURRENCY_CODE;
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
    price: [0, [Validators.required]],
  });

  constructor() {
    effect(() => {
      if (this.open()) {
        const product = this.product();
        this.form.reset({
          name: product?.name ?? '',
          description: product?.description ?? '',
          price: product?.price ?? 0,
        });
        this.error.set(null);
      }
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request = {
      name: value.name,
      description: value.description.trim() || null,
      price: Number(value.price),
    };
    const existing = this.product();
    this.saving.set(true);
    this.error.set(null);

    (existing ? this.products.update(existing.id, request) : this.products.create(request)).subscribe({
      next: (product) => {
        this.saving.set(false);
        this.saved.emit(product);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(describeHttpError(error));
      },
    });
  }
}
