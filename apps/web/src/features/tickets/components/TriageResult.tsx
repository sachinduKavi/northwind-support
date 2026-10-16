import { formatDateTime } from '../../../lib/format';
import type { CreatedTicket } from '../../../types/api';
import { SlaIndicator } from './SlaIndicator';
import { PriorityBadge } from './TicketBadges';

interface TriageResultProps {
  created: CreatedTicket;
}

export function TriageResult({ created }: TriageResultProps) {
  const { ticket, triage } = created;

  return (
    <dl className="summary">
      <dt>Priority</dt>
      <dd>
        <PriorityBadge priority={ticket.priority} />
        <p className="summary__note">{triage.priorityReason}</p>
      </dd>

      <dt>Due</dt>
      <dd>
        {formatDateTime(ticket.dueAtUtc)}
        <p className="summary__note">{triage.slaWindowReason}</p>
      </dd>

      <dt>SLA</dt>
      <dd>
        <SlaIndicator dueAtUtc={ticket.dueAtUtc} slaStatus={ticket.slaStatus} />
      </dd>

      <dt>Assigned to</dt>
      <dd>
        {ticket.assignedAgent?.fullName ?? 'Unassigned'}
        <p className="summary__note">{triage.assignmentReason}</p>
      </dd>
    </dl>
  );
}
