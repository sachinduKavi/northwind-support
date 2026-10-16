import { humanize } from '../../lib/format';
import type { TicketDetail } from '../../types/api';

export const reasonMinLength = 5;
export const reasonMaxLength = 500;

export function validateEscalationReason(reason: string): string | undefined {
  const length = reason.trim().length;

  return length < reasonMinLength || length > reasonMaxLength
    ? `Give a reason of ${reasonMinLength} to ${reasonMaxLength} characters.`
    : undefined;
}

export function escalationBlocker(ticket: Pick<TicketDetail, 'status' | 'priority'>): string | undefined {
  if (ticket.status === 'Resolved' || ticket.status === 'Closed') {
    return `This ticket is ${humanize(ticket.status).toLowerCase()} and cannot be escalated.`;
  }

  if (ticket.priority === 'Critical') {
    return 'This ticket is already Critical and cannot be escalated further.';
  }

  return undefined;
}
