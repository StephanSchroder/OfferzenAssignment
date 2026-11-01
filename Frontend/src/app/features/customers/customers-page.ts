import { Component, inject, signal, viewChild } from '@angular/core';
import { describeHttpError } from '../../core/http-error';
import { Customer } from '../../core/models/customer';
import { CustomerService } from '../../core/services/customer.service';
import { DataTable } from '../../shared/data-table/data-table';
import { ColumnDef } from '../../shared/data-table/data-table.models';
import { CustomerForm } from './customer-form';

@Component({
  selector: 'app-customers-page',
  imports: [DataTable, CustomerForm],
  template: `
    @if (error(); as message) {
      <p class="form-error" role="alert">{{ message }}</p>
    }
    <app-data-table
      entitySet="Customers"
      heading="Customers"
      itemName="customer"
      [columns]="columns"
      [defaultSort]="{ field: 'name', direction: 'asc' }"
      [deleteConfirmation]="deleteMessage"
      (add)="openForm(null)"
      (update)="openForm($event)"
      (deleteRow)="delete($event)"
    />
    <app-customer-form [open]="formOpen()" [customer]="editing()" (saved)="onSaved()" (closed)="formOpen.set(false)" />
  `,
})
export class CustomersPage {
  private readonly customers = inject(CustomerService);
  private readonly table = viewChild.required(DataTable<Customer>);

  protected readonly columns: ColumnDef<Customer>[] = [
    { field: 'id', header: 'ID', type: 'number', width: '130px' },
    { field: 'name', header: 'Name' },
    { field: 'email', header: 'Email' },
  ];

  protected readonly formOpen = signal(false);
  protected readonly editing = signal<Customer | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly deleteMessage = (customer: Customer) =>
    `${customer.name} will be removed from the customer list. Their purchase order history is kept.`;

  protected openForm(customer: Customer | null): void {
    this.editing.set(customer);
    this.formOpen.set(true);
  }

  protected onSaved(): void {
    this.formOpen.set(false);
    this.table().reload();
  }

  protected delete(customer: Customer): void {
    this.error.set(null);
    this.customers.delete(customer.id).subscribe({
      next: () => this.table().reload(),
      error: (error: unknown) => this.error.set(describeHttpError(error)),
    });
  }
}
