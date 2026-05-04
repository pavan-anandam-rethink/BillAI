import { AcknowledgeableException } from './acknowledgeable-exception';

export interface SaveAcknowledgeableExceptionsResult {
    acknowledgeableExceptions: AcknowledgeableException[],
    appointmentId: number,
    success: boolean,
}
