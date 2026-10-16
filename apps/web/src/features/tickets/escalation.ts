import { humanize } from '../../lib/format';
import type { TicketDetail } from '../../types/api';

export const reasonMinLength = 5;
export const reasonMaxLength = 500;
export const escalatedByMaxLength = 100;

export interface EscalationForm {
  reason: string;
  escalatedBy: string;
}

export function validateEscalation(form: EscalationForm): Partial<Record<keyof EscalationForm, string>> {
  const errors: Partial<Record<keyof EscalationForm, string>> = {};
  const reasonLength = form.reason.trim().length;

  if (reasonLength < reasonMinLength || reasonLength > reasonMaxLength) {
    errors.reason = `Give a reason of ${reasonMinLength} to ${reasonMaxLength} characters.`;
  }

  if (form.escalatedBy.trim() === '') {
    errors.escalatedBy = 'Say who is escalating the ticket.';
  } else if (form.escalatedBy.trim().length > escalatedByMaxLength) {
    errors.escalatedBy = `Keep this to ${escalatedByMaxLength} characters or fewer.`;
  }

  return errors;
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
