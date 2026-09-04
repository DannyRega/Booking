export interface Booking {
  id: number;
  sessionId: number;
  userId: number;
  createdAt: string;
}

export interface CreateBookingPayload {
  sessionId: number;
}