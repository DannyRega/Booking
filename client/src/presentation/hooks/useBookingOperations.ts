import { useState, useCallback } from 'react';
import type { IBookingRepository } from '../../infrastructure/repositories/BookingRepository';
import { ApiError } from '../../domain/models/Error';
import type { ToastMessage } from '../components/Toast';

export const useBookingOperations = (
  bookingRepo: IBookingRepository,
  onOperationSuccess: () => void
) => {
  const [isProcessing, setIsProcessing] = useState(false);
  const [toast, setToast] = useState<ToastMessage | null>(null);

  const clearToast = useCallback(() => setToast(null), []);

  const reserve = useCallback(
    async (sessionId: number): Promise<number | null> => {
      setIsProcessing(true);
      setToast(null);

      const idempotencyKey = crypto.randomUUID();

      try {
        const createdBooking = await bookingRepo.createBooking({ sessionId }, idempotencyKey);
        setToast({
          id: crypto.randomUUID(),
          type: 'success',
          message: `¡Reserva #${createdBooking.id} confirmada con éxito para la sesión #${sessionId}!`,
        });
        onOperationSuccess();
        return createdBooking.id;
      } catch (err: unknown) {
        if (err instanceof ApiError) {
          setToast({
            id: crypto.randomUUID(),
            type: 'error',
            message:
              err.statusCode === 409
                ? `${err.message}`
                : err.message,
          });
        } else if (err instanceof Error) {
          setToast({
            id: crypto.randomUUID(),
            type: 'error',
            message: err.message,
          });
        } else {
          setToast({
            id: crypto.randomUUID(),
            type: 'error',
            message: 'Error inesperado al procesar la reserva.',
          });
        }
        return null;
      } finally {
        setIsProcessing(false);
      }
    },
    [bookingRepo, onOperationSuccess]
  );

  const cancel = useCallback(
    async (bookingId: number): Promise<boolean> => {
      setIsProcessing(true);
      setToast(null);

      try {
        await bookingRepo.cancelBooking(bookingId);
        setToast({
          id: crypto.randomUUID(),
          type: 'info',
          message: `Reserva #${bookingId} cancelada exitosamente. Cupo liberado.`,
        });
        onOperationSuccess();
        return true;
      } catch (err: unknown) {
        if (err instanceof ApiError) {
          setToast({
            id: crypto.randomUUID(),
            type: 'error',
            message:
              err.statusCode === 409
                ? `${err.message}`
                : err.message,
          });
        } else if (err instanceof Error) {
          setToast({
            id: crypto.randomUUID(),
            type: 'error',
            message: err.message,
          });
        } else {
          setToast({
            id: crypto.randomUUID(),
            type: 'error',
            message: 'Error al cancelar la reserva.',
          });
        }
        return false;
      } finally {
        setIsProcessing(false);
      }
    },
    [bookingRepo, onOperationSuccess]
  );

  return { reserve, cancel, isProcessing, toast, clearToast };
};