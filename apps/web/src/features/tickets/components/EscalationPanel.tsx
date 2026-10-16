import { useState } from 'react';
import { ApiError, toErrorMessage } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { Field } from '../../../components/Field';
import type { EscalationResult, TicketDetail } from '../../../types/api';
import {
  escalatedByMaxLength,
  escalationBlocker,
  reasonMaxLength,
  reasonMinLength,
  validateEscalation,
  type EscalationForm,
} from '../escalation';

interface EscalationPanelProps {
  ticket: TicketDetail;
  onEscalated: (result: EscalationResult) => void;
}

const emptyForm: EscalationForm = { reason: '', escalatedBy: '' };

export function EscalationPanel({ ticket, onEscalated }: EscalationPanelProps) {
  const [form, setForm] = useState<EscalationForm>(emptyForm);
  const [errors, setErrors] = useState<Partial<Record<keyof EscalationForm, string>>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string>();
  const [outcome, setOutcome] = useState<string>();

  const blocker = escalationBlocker(ticket);

  const update = (patch: Partial<EscalationForm>) => setForm((current) => ({ ...current, ...patch }));

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationErrors = validateEscalation(form);
    setErrors(validationErrors);
    setSubmitError(undefined);
    setOutcome(undefined);

    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    setIsSaving(true);

    try {
      const result = await ticketsApi.escalate(ticket.id, {
        reason: form.reason.trim(),
        escalatedBy: form.escalatedBy.trim(),
      });

      setForm(emptyForm);
      setOutcome(
        `Escalated from ${result.escalation.fromPriority} to ${result.escalation.toPriority}. ${result.triage.assignmentReason}`,
      );
      onEscalated(result);
    } catch (caught) {
      if (caught instanceof ApiError) {
        const fieldErrors = caught.fieldErrors;
        setErrors({ reason: fieldErrors.Reason?.[0], escalatedBy: fieldErrors.EscalatedBy?.[0] });
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
        error={errors.reason}
        hint={`${reasonMinLength} to ${reasonMaxLength} characters. Raises the priority one level and restarts the SLA.`}
      >
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            rows={3}
            value={form.reason}
            maxLength={reasonMaxLength}
            disabled={isDisabled}
            onChange={(event) => update({ reason: event.target.value })}
          />
        )}
      </Field>

      <Field id="escalation-by" label="Escalated by" error={errors.escalatedBy}>
        {(fieldProps) => (
          <input
            {...fieldProps}
            type="text"
            value={form.escalatedBy}
            maxLength={escalatedByMaxLength}
            disabled={isDisabled}
            onChange={(event) => update({ escalatedBy: event.target.value })}
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
