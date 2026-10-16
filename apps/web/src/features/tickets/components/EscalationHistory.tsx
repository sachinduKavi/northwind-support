import { ErrorMessage } from '../../../components/ErrorMessage';
import { Spinner } from '../../../components/Spinner';
import { formatDateTime } from '../../../lib/format';
import type { AgentSummary, TicketEscalation } from '../../../types/api';
import { PriorityBadge } from './TicketBadges';

interface EscalationHistoryProps {
  escalations?: TicketEscalation[];
  isLoading: boolean;
  error?: string;
  onRetry: () => void;
}

const agentName = (agent: AgentSummary | null) => agent?.fullName ?? 'Unassigned';

export function EscalationHistory({ escalations, isLoading, error, onRetry }: EscalationHistoryProps) {
  return (
    <section className="card" aria-labelledby="escalation-history-heading">
      <h2 id="escalation-history-heading">Escalation history</h2>

      {error && <ErrorMessage message={error} onRetry={onRetry} />}

      {isLoading && !escalations && <Spinner label="Loading escalation history" />}

      {escalations && escalations.length === 0 && <p className="field__hint">This ticket has not been escalated.</p>}

      {escalations && escalations.length > 0 && (
        <ol className="escalation-history">
          {escalations.map((escalation) => (
            <li key={escalation.id} className="escalation-history__item">
              <p>
                <strong>{escalation.escalatedBy}</strong> on {formatDateTime(escalation.escalatedAtUtc)}
              </p>
              <dl className="summary">
                <dt>Priority</dt>
                <dd>
                  <PriorityBadge priority={escalation.fromPriority} /> to{' '}
                  <PriorityBadge priority={escalation.toPriority} />
                </dd>

                <dt>Agent</dt>
                <dd>
                  {agentName(escalation.fromAgent)} to {agentName(escalation.toAgent)}
                </dd>

                <dt>Due</dt>
                <dd>
                  {formatDateTime(escalation.fromDueAtUtc)} to {formatDateTime(escalation.toDueAtUtc)}
                </dd>

                <dt>Reason</dt>
                <dd>{escalation.reason}</dd>
              </dl>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}
