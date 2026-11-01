import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface NavItem {
  path: string;
  label: string;
}

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="navbar" aria-label="Main">
      <a class="brand" routerLink="/">
        <svg class="brand__mark" viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
          <rect x="3" y="3" width="18" height="18" rx="3" fill="none" stroke="currentColor" stroke-width="1.8" />
          <path d="M7 9h10M7 13h10M7 17h6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
        </svg>
        Order desk
      </a>
      <ul class="nav-links">
        @for (item of items; track item.path) {
          <li>
            <a [routerLink]="item.path" routerLinkActive="is-active" ariaCurrentWhenActive="page">{{ item.label }}</a>
          </li>
        }
      </ul>
    </nav>
  `,
  styles: `
    .navbar {
      display: flex;
      align-items: stretch;
      gap: 40px;
      padding: 0 28px;
      height: 60px;
      background: var(--ink);
      color: #e7edf2;
    }
    .brand {
      display: inline-flex;
      align-items: center;
      gap: 10px;
      color: #fff;
      font-weight: 700;
      font-size: 1.0625rem;
      letter-spacing: -0.01em;
      text-decoration: none;
    }
    .brand__mark { color: #8fc1e3; }
    .nav-links { display: flex; gap: 4px; margin: 0; padding: 0; list-style: none; }
    .nav-links li { display: flex; }
    .nav-links a {
      display: flex;
      align-items: center;
      padding: 0 14px;
      color: #b8c4cf;
      text-decoration: none;
      font-weight: 500;
      border-bottom: 3px solid transparent;
      border-top: 3px solid transparent;
    }
    .nav-links a:hover { color: #fff; }
    .nav-links a.is-active { color: #fff; border-bottom-color: #8fc1e3; }
    .nav-links a:focus-visible, .brand:focus-visible { outline: 2px solid #8fc1e3; outline-offset: -2px; }
    @media (max-width: 640px) {
      .navbar { flex-direction: column; height: auto; gap: 0; padding: 0 16px; }
      .brand { padding: 14px 0 6px; }
      .nav-links { overflow-x: auto; margin: 0 -8px; }
      .nav-links a { padding: 10px 8px; }
    }
  `,
})
export class Navbar {
  protected readonly items: NavItem[] = [
    { path: '/customers', label: 'Customers' },
    { path: '/purchase-orders', label: 'Purchase orders' },
    { path: '/products', label: 'Products' },
  ];
}
