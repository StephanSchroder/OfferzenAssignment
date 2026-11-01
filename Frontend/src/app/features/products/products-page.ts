import { Component, inject, signal, viewChild } from '@angular/core';
import { describeHttpError } from '../../core/http-error';
import { Product } from '../../core/models/product';
import { ProductService } from '../../core/services/product.service';
import { DataTable } from '../../shared/data-table/data-table';
import { ColumnDef } from '../../shared/data-table/data-table.models';
import { ProductForm } from './product-form';

@Component({
  selector: 'app-products-page',
  imports: [DataTable, ProductForm],
  template: `
    @if (error(); as message) {
      <p class="form-error" role="alert">{{ message }}</p>
    }
    <app-data-table
      entitySet="Products"
      heading="Products"
      itemName="product"
      [columns]="columns"
      [defaultSort]="{ field: 'name', direction: 'asc' }"
      [deleteConfirmation]="deleteMessage"
      (add)="openForm(null)"
      (update)="openForm($event)"
      (deleteRow)="delete($event)"
    />
    <app-product-form [open]="formOpen()" [product]="editing()" (saved)="onSaved()" (closed)="formOpen.set(false)" />
  `,
})
export class ProductsPage {
  private readonly products = inject(ProductService);
  private readonly table = viewChild.required(DataTable<Product>);

  protected readonly columns: ColumnDef<Product>[] = [
    { field: 'id', header: 'ID', type: 'number', width: '130px' },
    { field: 'name', header: 'Name' },
    { field: 'description', header: 'Description' },
    { field: 'price', header: 'Price', type: 'currency', width: '170px' },
  ];

  protected readonly formOpen = signal(false);
  protected readonly editing = signal<Product | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly deleteMessage = (product: Product) =>
    `${product.name} will no longer be available for new purchase orders. Existing orders keep it.`;

  protected openForm(product: Product | null): void {
    this.editing.set(product);
    this.formOpen.set(true);
  }

  protected onSaved(): void {
    this.formOpen.set(false);
    this.table().reload();
  }

  protected delete(product: Product): void {
    this.error.set(null);
    this.products.delete(product.id).subscribe({
      next: () => this.table().reload(),
      error: (error: unknown) => this.error.set(describeHttpError(error)),
    });
  }
}
