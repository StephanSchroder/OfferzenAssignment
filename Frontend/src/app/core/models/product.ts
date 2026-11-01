export interface Product {
  id: number;
  name: string;
  description: string | null;
  price: number;
}

export interface ProductRequest {
  name: string;
  description: string | null;
  price: number;
}
