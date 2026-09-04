import type { Booking, CreateBookingPayload } from '../../domain/models/Booking';
import { HttpClient } from '../api/HttpClient';

export interface IBookingRepository {
  createBooking(payload: CreateBookingPayload, idempotencyKey: string): Promise<Booking>;
  cancelBooking(bookingId: number): Promise<void>;
}

export class BookingRepository implements IBookingRepository {
  async createBooking(payload: CreateBookingPayload, idempotencyKey: string): Promise<Booking> {
    return HttpClient.request<Booking>('/bookings', {
      method: 'POST',
      headers: {
        'Idempotency-Key': idempotencyKey,
      },
      body: JSON.stringify(payload),
    });
  }

  async cancelBooking(bookingId: number): Promise<void> {
    return HttpClient.request<void>(`/bookings/${bookingId}`, {
      method: 'DELETE',
    });
  }
}