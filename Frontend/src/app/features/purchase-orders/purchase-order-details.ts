import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, effect, inject, input, output, signal } from '@angular/core';
import { CURRENCY_CODE } from '../../core/api.config';
import { describeHttpError } from '../../core/http-error';
import { PurchaseOrderDetails as Details } from '../../core/models/purchase-order';
import { PurchaseOrderService } from '../../core/services/purchase-order.service';
import { Modal } from '../../shared/modal/modal';

/** Read-only view of one purchase order: customer, items, prices at the time and the backend total. */
@Component({
  selector: 'app-purchase-order-details',
  imports: [CurrencyPipe, DatePipe, Modal],
  template: `
    <app-modal
      size="wide"
      [open]="open()"
      [heading]="order() ? 'Purchase order ' + order()!.clientReference : 'Purchase order'"
      (closed)="closed.emit()"
    >
      @if (loading()) {
        <p class="muted">Loading…</p>
      } @else if (error(); as message) {
        <p class="form-error" role="alert">{{ message }}</p>
      } @else if (order(); as po) {
        <dl class="summary">
          <div>
            <dt>Customer</dt>
            <dd>
              <span class="customer-name">{{ po.customerName }}</span>
              <span class="muted">{{ po.customerEmail }}</span>
            </dd>
          </div>
          <div>
            <dt>Ordered</dt>
            <dd>{{ po.timestamp | date: 'd MMM y, HH:mm' }}</dd>
          </div>
          <div>
            <dt>Status</dt>
            <dd>
              <span class="pill" [attr.data-value]="po.status">{{ po.status }}</span>
              @if (po.isArchived) {
                <span class="archived">Archived {{ po.archivedAt | date: 'd MMM y' }}</span>
              }
            </dd>
          </div>
        </dl>

        <table class="items">
          <thead>
            <tr>
              <th scope="col">Product</th>
              <th scope="col" class="num">Quantity</th>
              <th scope="col" class="num">Unit price at order</th>
              <th scope="col" class="num">Line total</th>
            </tr>
          </thead>
          <tbody>
            @for (item of po.items; track item.id) {
              <tr>
                <td>{{ item.productName }}</td>
                <td class="num">{{ item.quantity }}</td>
                <td class="num">{{ item.unitPrice | currency: currency }}</td>
                <td class="num">{{ item.lineTotal | currency: currency }}</td>
              </tr>
            }
          </tbody>
          <tfoot>
            <tr>
              <th scope="row" colspan="3">Total</th>
              <td class="num total">{{ po.total | currency: currency }}</td>
            </tr>
          </tfoot>
        </table>
      }

      <div modal-footer>
        @if (order(); as po) {
          @if (!po.isArchived) {
            <button type="button" class="button button--secondary" (click)="update.emit(po.id)">Update purchase order</button>
          }
        }
        <button type="button" class="button button--primary" (click)="closed.emit()">Close</button>
      </div>
    </app-modal>
  `,
  styles: `
    .summary {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: 16px 24px;
      margin: 0 0 22px;
    }
    .summary dt { font-size: 0.8125rem; color: var(--muted); margin-bottom: 4px; }
    .summary dd { margin: 0; display: flex; flex-direction: column; gap: 2px; align-items: flex-start; }
    .customer-name { font-weight: 600; font-size: 1.0625rem; }
    .archived { font-size: 0.8125rem; color: var(--muted); }
    .muted { color: var(--muted); }
    .items { width: 100%; border-collapse: collapse; font-size: 0.9375rem; }
    .items th { text-align: left; font-size: 0.8125rem; font-weight: 600; color: var(--muted); padding: 8px 10px; border-bottom: 1px solid var(--line); }
    .items td { padding: 10px; border-bottom: 1px solid var(--line-soft); }
    .items th:first-child, .items td:first-child { padding-left: 0; }
    .items th:last-child, .items td:last-child { padding-right: 0; }
    .items .num { text-align: right; font-variant-numeric: tabular-nums; white-space: nowrap; }
    .items tfoot th { text-align: right; color: var(--ink); font-size: 0.9375rem; border-bottom: none; padding-top: 14px; }
    .items tfoot td { border-bottom: none; padding-top: 14px; }
    .items .total { font-size: 1.25rem; font-weight: 700; }
  `,
})
export class PurchaseOrderDetailsModal {
  private readonly orders = inject(PurchaseOrderService);

  readonly open = input(false);
  readonly orderId = input<number | null>(null);
  readonly closed = output<void>();
  readonly update = output<number>();

  protected readonly currency = CURRENCY_CODE;
  protected readonly order = signal<Details | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => {
      const id = this.orderId();
      if (this.open() && id != null) {
        this.load(id);
      }
    });
  }

  private load(id: number): void {
    this.loading.set(true);
    this.error.set(null);
    this.order.set(null);
    this.orders.get(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(describeHttpError(error));
        this.loading.set(false);
      },
    });
  }
}
