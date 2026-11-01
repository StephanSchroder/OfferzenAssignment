export const PURCHASE_ORDER_STATUSES = ['Pending', 'Confirmed', 'Fulfilled', 'Cancelled'] as const;

export type PurchaseOrderStatus = (typeof PURCHASE_ORDER_STATUSES)[number];

/** Row returned by /odata/PurchaseOrders. */
export interface PurchaseOrderSummary {
  id: number;
  clientReference: string;
  customerId: number;
  customerName: string;
  timestamp: string;
  status: PurchaseOrderStatus;
  itemCount: number;
  total: number;
  isArchived: boolean;
}

export interface PurchaseOrderItem {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  /** Price per unit at the time the line was ordered. */
  unitPrice: number;
  lineTotal: number;
}

export interface PurchaseOrderDetails {
  id: number;
  clientReference: string;
  customerId: number;
  customerName: string;
  customerEmail: string;
  timestamp: string;
  status: PurchaseOrderStatus;
  isArchived: boolean;
  archivedAt: string | null;
  items: PurchaseOrderItem[];
  /** Calculated by the backend. */
  total: number;
}

export interface PurchaseOrderItemRequest {
  id: number | null;
  productId: number;
  quantity: number;
}

export interface PurchaseOrderRequest {
  customerId: number;
  clientReference: string;
  status: PurchaseOrderStatus | null;
  items: PurchaseOrderItemRequest[];
}
