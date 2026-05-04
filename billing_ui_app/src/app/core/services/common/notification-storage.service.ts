import { Injectable } from '@angular/core';

const STORAGE_KEY = 'billing_pusher_notifications';
const EXPIRATION_MS = 3 * 60 * 1000; // 3 minutes

export interface StoredNotification {
  data: any;
  createdAt: number; // epoch ms
}

@Injectable({
  providedIn: 'root'
})
export class NotificationStorageService {

  /** Load non-expired notifications from localStorage */
  loadNotifications(): any[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return [];

      const stored: StoredNotification[] = JSON.parse(raw);
      const now = Date.now();
      const active = stored.filter(s => (now - s.createdAt) < EXPIRATION_MS);

      // Prune expired entries from storage
      if (active.length !== stored.length) {
        this.writeToStorage(active);
      }

      // Restore Date objects for the time property
      return active.map(s => {
        const notification = { ...s.data };
        if (notification.time) {
          notification.time = new Date(notification.time);
        }
        return notification;
      });
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return [];
    }
  }

  /** Save the current notifications array to localStorage */
  saveNotifications(notifications: any[]): void {
    const now = Date.now();

    // Load existing entries to preserve original creation timestamps
    let existingMap = new Map<string, number>();
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const stored: StoredNotification[] = JSON.parse(raw);
        stored.forEach(s => {
          const key = this.getNotificationKey(s.data);
          existingMap.set(key, s.createdAt);
        });
      }
    } catch { /* ignore */ }

    const toStore: StoredNotification[] = notifications
      .filter(n => !n.title?.includes('Unprocessed Appointments')) // Don't persist dynamic appointment notifications
      .map(n => {
        const key = this.getNotificationKey(n);
        return {
          data: n,
          createdAt: existingMap.get(key) ?? now
        };
      })
      .filter(s => (now - s.createdAt) < EXPIRATION_MS); // Drop already-expired

    this.writeToStorage(toStore);
  }

  /** Remove a specific notification from storage */
  removeNotification(notification: any): void {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return;

      const stored: StoredNotification[] = JSON.parse(raw);
      const key = this.getNotificationKey(notification);
      const filtered = stored.filter(s => this.getNotificationKey(s.data) !== key);
      this.writeToStorage(filtered);
    } catch { /* ignore */ }
  }

  /** Clear all stored notifications */
  clear(): void {
    localStorage.removeItem(STORAGE_KEY);
  }

  /** Calculate remaining TTL (ms) for a notification based on its stored createdAt */
  getRemainingTtl(notification: any): number {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return EXPIRATION_MS;

      const stored: StoredNotification[] = JSON.parse(raw);
      const key = this.getNotificationKey(notification);
      const entry = stored.find(s => this.getNotificationKey(s.data) === key);
      if (entry) {
        const remaining = EXPIRATION_MS - (Date.now() - entry.createdAt);
        return remaining > 0 ? remaining : 0;
      }
    } catch { /* ignore */ }
    return EXPIRATION_MS;
  }

  private writeToStorage(entries: StoredNotification[]): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(entries));
  }

  private getNotificationKey(notification: any): string {
    return `${notification.batchId ?? ''}_${notification.title ?? ''}`;
  }
}
