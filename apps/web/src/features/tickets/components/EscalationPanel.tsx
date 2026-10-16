import { useState } from 'react';
import { ApiError, toErrorMessage } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { Field } from '../../../components/Field';
import type { EscalationResult, TicketDetail } from '../../../types/api';
import { escalationBlocker, reasonMaxLength, reasonMinLength, validateEscalationReason } from '../escalation';

interface EscalationPanelProps {
  ticket: TicketDetail;
  onEscalated: (result: EscalationResult) => void;
}

export function EscalationPanel({ ticket, onEscalated }: EscalationPanelProps) {
  const [reason, setReason] = useState('');
  const [reasonError, setReasonError] = useState<string>();
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string>();
  const [outcome, setOutcome] = useState<string>();

  const blocker = escalationBlocker(ticket);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationError = validateEscalationReason(reason);
    setReasonError(validationError);
    setSubmitError(undefined);
    setOutcome(undefined);

    if (validationError) {
      return;
    }

    setIsSaving(true);

    try {
      const result = await ticketsApi.escalate(ticket.id, { reason: reason.trim() });

      setReason('');
      setOutcome(
        `Escalated from ${result.escalation.fromPriority} to ${result.escalation.toPriority}. ${result.triage.assignmentReason}`,
      );
      onEscalated(result);
    } catch (caught) {
      if (caught instanceof ApiError) {
        setReasonError(caught.fieldErrors.Reason?.[0]);
      }

      setSubmitError(toErrorMessage(caught, 'Could not escalate the ticket.'));
    } finally {
      setIsSaving(false);
    }
  };

  const isDisabled = blocker !== undefined || isSaving;

  return (
    <form className="card form" onSubmit={submit} noValidate aria-labelledby="escalate-heading">
      <h2 id="escalate-heading">Escalate</h2>

      {blocker && <p className="field__hint">{blocker}</p>}

      <Field
        id="escalation-reason"
        label="Reason"
        error={reasonError}
        hint={`${reasonMinLength} to ${reasonMaxLength} characters. Raises the priority one level and restarts the SLA.`}
      >
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            rows={3}
            value={reason}
            maxLength={reasonMaxLength}
            disabled={isDisabled}
            onChange={(event) => setReason(event.target.value)}
          />
        )}
      </Field>

      {submitError && (
        <p className="field__error" role="alert">
          {submitError}
        </p>
      )}

      {outcome && (
        <p className="field__hint" role="status">
          {outcome}
        </p>
      )}

      <div className="button-row">
        <button type="submit" className="button button--primary" disabled={isDisabled}>
          {isSaving ? 'Escalating...' : 'Escalate ticket'}
        </button>
      </div>
    </form>
  );
}
