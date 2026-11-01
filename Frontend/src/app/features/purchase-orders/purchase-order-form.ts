import { CurrencyPipe } from '@angular/common';
import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { CURRENCY_CODE } from '../../core/api.config';
import { describeHttpError, fieldErrors } from '../../core/http-error';
import { Customer } from '../../core/models/customer';
import { Product } from '../../core/models/product';
import {
  PURCHASE_ORDER_STATUSES,
  PurchaseOrderDetails,
  PurchaseOrderRequest,
  PurchaseOrderStatus,
} from '../../core/models/purchase-order';
import { statusChangeProblem, statusHint } from '../../core/models/purchase-order-status-rules';
import { CustomerService } from '../../core/services/customer.service';
import { ProductService } from '../../core/services/product.service';
import { PurchaseOrderService } from '../../core/services/purchase-order.service';
import { Modal } from '../../shared/modal/modal';

type LineForm = FormGroup<{
  id: FormControl<number | null>;
  productId: FormControl<number | null>;
  quantity: FormControl<number>;
  /** Price captured on the order (existing lines only). */
  orderedPrice: FormControl<number | null>;
  orderedProductId: FormControl<number | null>;
  orderedProductName: FormControl<string | null>;
}>;

/** Add / update purchase order modal. Pass `orderId` to update, leave it null to add. */
@Component({
  selector: 'app-purchase-order-form',
  imports: [ReactiveFormsModule, CurrencyPipe, Modal],
  templateUrl: './purchase-order-form.html',
  styleUrl: './purchase-order-form.css',
})
export class PurchaseOrderForm {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly orders = inject(PurchaseOrderService);
  private readonly customerService = inject(CustomerService);
  private readonly productService = inject(ProductService);

  readonly open = input(false);
  readonly orderId = input<number | null>(null);
  readonly saved = output<PurchaseOrderDetails>();
  readonly closed = output<void>();

  protected readonly currency = CURRENCY_CODE;
  protected readonly statuses = PURCHASE_ORDER_STATUSES;
  protected readonly customers = signal<Customer[]>([]);
  protected readonly products = signal<Product[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly order = signal<PurchaseOrderDetails | null>(null);

  protected readonly form = this.fb.group({
    customerId: this.fb.control<number | null>(null, Validators.required),
    clientReference: ['', [Validators.required, Validators.maxLength(100)]],
    status: this.fb.control<PurchaseOrderStatus>('Pending'),
    items: this.fb.array<LineForm>([]),
  });

  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  private readonly productsById = computed(() => new Map(this.products().map((p) => [p.id, p])));

  /** Status the order had when the form was opened (null when adding). */
  protected readonly originalStatus = computed(() => this.order()?.status ?? null);

  /** Message shown as soon as the user picks a status change that doesn't make sense. */
  protected readonly statusProblem = computed(() => {
    this.formValue();
    const from = this.originalStatus();
    return from ? statusChangeProblem(from, this.form.controls.status.value) : null;
  });

  protected readonly statusHint = computed(() => {
    const from = this.originalStatus();
    return from ? statusHint(from) : null;
  });

  /** Status error returned by the backend (it validates the same rules). */
  protected readonly serverStatusError = signal<string | null>(null);

  /** Client-side preview only; the backend calculates the real total when saving. */
  protected readonly estimatedTotal = computed(() => {
    this.formValue();
    return this.lines.controls.reduce((sum, line) => sum + this.linePrice(line) * (line.controls.quantity.value || 0), 0);
  });

  protected get lines(): FormArray<LineForm> {
    return this.form.controls.items;
  }

  constructor() {
    effect(() => {
      if (this.open()) {
        this.load(this.orderId());
      }
    });
  }

  protected addLine(): void {
    this.lines.push(this.createLine());
  }

  protected removeLine(index: number): void {
    this.lines.removeAt(index);
  }

  /** Price that applies to a line: the captured price for unchanged existing lines, otherwise the current price. */
  protected linePrice(line: LineForm): number {
    const { productId, orderedPrice, orderedProductId } = line.getRawValue();
    if (orderedPrice != null && productId === orderedProductId) {
      return orderedPrice;
    }
    return productId != null ? (this.productsById().get(productId)?.price ?? 0) : 0;
  }

  protected isCapturedPrice(line: LineForm): boolean {
    const { orderedPrice, productId, orderedProductId } = line.getRawValue();
    return orderedPrice != null && productId === orderedProductId;
  }

  /** Products shown in a line's dropdown; keeps a since-deleted product visible on existing lines. */
  protected productOptions(line: LineForm): { id: number; name: string }[] {
    const { orderedProductId, orderedProductName } = line.getRawValue();
    const options = this.products().map((p) => ({ id: p.id, name: p.name }));
    if (orderedProductId != null && !this.productsById().has(orderedProductId)) {
      options.unshift({ id: orderedProductId, name: `${orderedProductName ?? 'Product'} (no longer sold)` });
    }
    return options;
  }

  protected save(): void {
    if (this.statusProblem()) {
      this.form.controls.status.markAsTouched();
      return;
    }

    if (this.form.invalid || this.lines.length === 0) {
      this.form.markAllAsTouched();
      if (this.lines.length === 0) this.error.set('Add at least one item.');
      return;
    }

    const value = this.form.getRawValue();
    const request: PurchaseOrderRequest = {
      customerId: value.customerId!,
      clientReference: value.clientReference.trim(),
      status: this.orderId() ? value.status : null,
      items: value.items.map((line) => ({ id: line.id, productId: line.productId!, quantity: Math.floor(line.quantity) })),
    };

    const id = this.orderId();
    this.saving.set(true);
    this.error.set(null);
    this.serverStatusError.set(null);
    (id ? this.orders.update(id, request) : this.orders.create(request)).subscribe({
      next: (order) => {
        this.saving.set(false);
        this.saved.emit(order);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        const statusErrors = fieldErrors(error, 'Status');
        if (statusErrors.length > 0) {
          this.serverStatusError.set(statusErrors.join(' '));
        } else {
          this.error.set(describeHttpError(error));
        }
      },
    });
  }

  private load(orderId: number | null): void {
    this.loading.set(true);
    this.error.set(null);
    this.serverStatusError.set(null);
    this.order.set(null);
    this.lines.clear();
    this.form.reset({ customerId: null, clientReference: '', status: 'Pending' });

    forkJoin({
      customers: this.customerService.getAll(),
      products: this.productService.getAll(),
      order: orderId ? this.orders.get(orderId) : of(null),
    }).subscribe({
      next: ({ customers, products, order }) => {
        this.products.set(products);
        this.customers.set(this.withOrderCustomer(customers, order));
        this.order.set(order);

        if (order) {
          this.form.patchValue({ customerId: order.customerId, clientReference: order.clientReference, status: order.status });
          for (const item of order.items) {
            this.lines.push(
              this.createLine({
                id: item.id,
                productId: item.productId,
                quantity: item.quantity,
                orderedPrice: item.unitPrice,
                orderedProductId: item.productId,
                orderedProductName: item.productName,
              }),
            );
          }
        } else {
          this.addLine();
        }
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.error.set(describeHttpError(error));
      },
    });
  }

  /** Keeps the order's customer selectable even if that customer has since been deleted. */
  private withOrderCustomer(customers: Customer[], order: PurchaseOrderDetails | null): Customer[] {
    if (!order || customers.some((c) => c.id === order.customerId)) return customers;
    return [{ id: order.customerId, name: `${order.customerName} (deleted)`, email: order.customerEmail }, ...customers];
  }

  private createLine(
    value: Partial<{
      id: number | null;
      productId: number | null;
      quantity: number;
      orderedPrice: number | null;
      orderedProductId: number | null;
      orderedProductName: string | null;
    }> = {},
  ): LineForm {
    return this.fb.group({
      id: this.fb.control<number | null>(value.id ?? null),
      productId: this.fb.control<number | null>(value.productId ?? null, Validators.required),
      quantity: this.fb.control(value.quantity ?? 1, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]),
      orderedPrice: this.fb.control<number | null>(value.orderedPrice ?? null),
      orderedProductId: this.fb.control<number | null>(value.orderedProductId ?? null),
      orderedProductName: this.fb.control<string | null>(value.orderedProductName ?? null),
    });
  }
}
