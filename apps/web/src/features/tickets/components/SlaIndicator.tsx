import { Badge } from '../../../components/Badge';
import { formatDateTime } from '../../../lib/format';
import type { SlaStatus } from '../../../types/api';
import { slaStates } from '../sla';

interface SlaIndicatorProps {
  dueAtUtc: string | null;
  slaStatus: SlaStatus;
}

export function SlaIndicator({ dueAtUtc, slaStatus }: SlaIndicatorProps) {
  const state = slaStates[slaStatus];

  return (
    <div className="sla-cell" data-sla-status={slaStatus}>
      <Badge tone={state.tone}>
        <span aria-hidden="true">{state.symbol} </span>
        <span>{state.label}</span>
      </Badge>
      <span className="sla-cell__due">{formatDateTime(dueAtUtc)}</span>
    </div>
  );
}
