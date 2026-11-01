import { Component, ElementRef, effect, input, output, viewChild } from '@angular/core';

/**
 * Modal dialog built on the native <dialog> element (focus trapping and Escape handling
 * come from the browser). Content is projected; put buttons in an element with [modal-footer].
 */
@Component({
  selector: 'app-modal',
  template: `
    <dialog
      #dialog
      class="modal"
      [class.modal--wide]="size() === 'wide'"
      [attr.aria-labelledby]="titleId"
      (cancel)="onCancel($event)"
      (click)="onBackdropClick($event)"
    >
      <div class="modal__panel">
        <header class="modal__header">
          <h2 class="modal__title" [id]="titleId">{{ heading() }}</h2>
          <button type="button" class="icon-button" (click)="closed.emit()" aria-label="Close">
            <svg viewBox="0 0 20 20" width="18" height="18" aria-hidden="true">
              <path d="M5 5l10 10M15 5L5 15" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
            </svg>
          </button>
        </header>
        <div class="modal__body">
          <ng-content />
        </div>
        <footer class="modal__footer">
          <ng-content select="[modal-footer]" />
        </footer>
      </div>
    </dialog>
  `,
  styles: `
    .modal {
      border: none;
      padding: 0;
      background: transparent;
      width: min(520px, calc(100vw - 32px));
      max-height: calc(100dvh - 48px);
      color: inherit;
    }
    .modal--wide { width: min(820px, calc(100vw - 32px)); }
    .modal::backdrop { background: rgb(22 32 43 / 0.45); }
    .modal[open] .modal__panel { animation: modal-in 160ms ease-out; }
    .modal__panel {
      background: var(--surface);
      border-radius: 10px;
      box-shadow: 0 24px 60px rgb(22 32 43 / 0.28);
      display: flex;
      flex-direction: column;
      max-height: calc(100dvh - 48px);
    }
    .modal__header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 18px 22px 14px;
      border-bottom: 1px solid var(--line);
    }
    .modal__title { margin: 0; font-size: 1.15rem; font-weight: 600; }
    .modal__body { padding: 18px 22px; overflow: auto; }
    .modal__footer {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
      padding: 14px 22px 18px;
      border-top: 1px solid var(--line);
    }
    .modal__footer:empty { display: none; }
    @keyframes modal-in {
      from { opacity: 0; transform: translateY(8px); }
      to { opacity: 1; transform: none; }
    }
    @media (prefers-reduced-motion: reduce) {
      .modal[open] .modal__panel { animation: none; }
    }
  `,
})
export class Modal {
  private static nextId = 0;

  readonly open = input(false);
  readonly heading = input('');
  readonly size = input<'normal' | 'wide'>('normal');
  readonly closed = output<void>();

  protected readonly titleId = `modal-title-${Modal.nextId++}`;
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const element = this.dialog().nativeElement;
      if (this.open() && !element.open) {
        element.showModal();
      } else if (!this.open() && element.open) {
        element.close();
      }
    });
  }

  protected onCancel(event: Event): void {
    // Escape: let the parent decide (it owns the `open` state).
    event.preventDefault();
    this.closed.emit();
  }

  protected onBackdropClick(event: MouseEvent): void {
    if (event.target === this.dialog().nativeElement) {
      this.closed.emit();
    }
  }
}
