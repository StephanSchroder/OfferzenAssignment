import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { CURRENCY_CODE } from '../../core/api.config';
import { describeHttpError } from '../../core/http-error';
import { PURCHASE_ORDER_STATUSES, PurchaseOrderSummary } from '../../core/models/purchase-order';
import { PurchaseOrderService } from '../../core/services/purchase-order.service';
import { DataTable } from '../../shared/data-table/data-table';
import { ColumnDef } from '../../shared/data-table/data-table.models';
import { PurchaseOrderDetailsModal } from './purchase-order-details';
import { PurchaseOrderForm } from './purchase-order-form';

@Component({
  selector: 'app-purchase-orders-page',
  imports: [DataTable, PurchaseOrderForm, PurchaseOrderDetailsModal],
  template: `
    @if (error(); as message) {
      <p class="form-error" role="alert">{{ message }}</p>
    }
    <app-data-table
      entitySet="PurchaseOrders"
      heading="Purchase orders"
      itemName="purchase order"
      deleteLabel="Archive"
      [columns]="columns()"
      [baseFilter]="showArchived() ? null : 'isArchived eq false'"
      [defaultSort]="{ field: 'timestamp', direction: 'desc' }"
      [deleteConfirmation]="archiveMessage"
      [canDelete]="canArchive"
      [rowClickSelects]="false"
      (add)="openForm(null)"
      (update)="openForm($event.id)"
      (deleteRow)="archive($event)"
      (rowClick)="openDetails($event.id)"
    >
      <label toolbar class="archived-toggle">
        <input type="checkbox" [checked]="showArchived()" (change)="showArchived.set($any($event.target).checked)" />
        Show archived
      </label>
    </app-data-table>
    <p class="hint">Click an order to see its items and total. Use the selector on the left to choose an order to update.</p>

    <app-purchase-order-form
      [open]="formOpen()"
      [orderId]="editingId()"
      (saved)="onSaved($event.id)"
      (closed)="formOpen.set(false)"
    />
    <app-purchase-order-details
      [open]="detailsId() !== null"
      [orderId]="detailsId()"
      (closed)="detailsId.set(null)"
      (update)="editFromDetails($event)"
    />
  `,
  styles: `
    .archived-toggle {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      font-size: 0.875rem;
      color: var(--muted);
      cursor: pointer;
    }
    .archived-toggle input { accent-color: var(--primary); width: 16px; height: 16px; margin: 0; }
    .hint { color: var(--muted); font-size: 0.8125rem; margin: 10px 4px 0; }
  `,
})
export class PurchaseOrdersPage {
  private readonly orders = inject(PurchaseOrderService);
  private readonly table = viewChild.required(DataTable<PurchaseOrderSummary>);

  protected readonly showArchived = signal(false);
  protected readonly formOpen = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected readonly detailsId = signal<number | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly columns = computed<ColumnDef<PurchaseOrderSummary>[]>(() => [
    { field: 'id', header: 'ID', type: 'number', width: '130px' },
    { field: 'clientReference', header: 'Client reference' },
    { field: 'customerName', header: 'Customer' },
    { field: 'timestamp', header: 'Ordered', type: 'date', width: '240px' },
    { field: 'status', header: 'Status', type: 'enum', options: PURCHASE_ORDER_STATUSES, width: '140px' },
    { field: 'itemCount', header: 'Items', type: 'number', width: '110px', filterable: false },
    { field: 'total', header: `Total (${CURRENCY_CODE})`, type: 'currency', width: '160px' },
    ...(this.showArchived()
      ? [{ field: 'isArchived', header: 'Archived', type: 'boolean', width: '110px' } as ColumnDef<PurchaseOrderSummary>]
      : []),
  ]);

  protected readonly canArchive = (order: PurchaseOrderSummary) => !order.isArchived;

  protected readonly archiveMessage = (order: PurchaseOrderSummary) =>
    `Purchase order ${order.clientReference} will be archived. It is kept and can still be viewed with "Show archived".`;

  protected openForm(id: number | null): void {
    this.editingId.set(id);
    this.formOpen.set(true);
  }

  protected openDetails(id: number): void {
    this.detailsId.set(id);
  }

  protected editFromDetails(id: number): void {
    this.detailsId.set(null);
    this.openForm(id);
  }

  protected onSaved(id: number): void {
    this.formOpen.set(false);
    this.table().reload();
    this.detailsId.set(id);
  }

  protected archive(order: PurchaseOrderSummary): void {
    this.error.set(null);
    this.orders.archive(order.id).subscribe({
      next: () => this.table().reload(),
      error: (error: unknown) => this.error.set(describeHttpError(error)),
    });
  }
}
