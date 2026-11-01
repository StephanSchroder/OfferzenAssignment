import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { PurchaseOrderDetails, PurchaseOrderRequest } from '../models/purchase-order';

@Injectable({ providedIn: 'root' })
export class PurchaseOrderService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API_BASE_URL}/api/purchaseorders`;

  get(id: number): Observable<PurchaseOrderDetails> {
    return this.http.get<PurchaseOrderDetails>(`${this.url}/${id}`);
  }

  create(request: PurchaseOrderRequest): Observable<PurchaseOrderDetails> {
    return this.http.post<PurchaseOrderDetails>(this.url, request);
  }

  update(id: number, request: PurchaseOrderRequest): Observable<PurchaseOrderDetails> {
    return this.http.put<PurchaseOrderDetails>(`${this.url}/${id}`, request);
  }

  /** Archives the order (it is never physically deleted). */
  archive(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
