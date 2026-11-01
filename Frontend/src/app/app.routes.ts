import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'purchase-orders' },
  {
    path: 'customers',
    title: 'Customers | Order desk',
    loadComponent: () => import('./features/customers/customers-page').then((m) => m.CustomersPage),
  },
  {
    path: 'purchase-orders',
    title: 'Purchase orders | Order desk',
    loadComponent: () => import('./features/purchase-orders/purchase-orders-page').then((m) => m.PurchaseOrdersPage),
  },
  {
    path: 'products',
    title: 'Products | Order desk',
    loadComponent: () => import('./features/products/products-page').then((m) => m.ProductsPage),
  },
  { path: '**', redirectTo: 'purchase-orders' },
];
