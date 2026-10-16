import { useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { ApiError, toErrorMessage } from '../../api/client';
import { Field } from '../../components/Field';
import { useAuth, type RedirectState } from './authContext';

interface LoginForm {
  email: string;
  password: string;
}

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function LoginPage() {
  const { agent, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as RedirectState | null)?.from ?? '/tickets';

  const [form, setForm] = useState<LoginForm>({ email: '', password: '' });
  const [errors, setErrors] = useState<Partial<Record<keyof LoginForm, string>>>({});
  const [isSigningIn, setIsSigningIn] = useState(false);
  const [submitError, setSubmitError] = useState<string>();

  if (agent) {
    return <Navigate to={from} replace />;
  }

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationErrors = validate(form);
    setErrors(validationErrors);
    setSubmitError(undefined);

    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    setIsSigningIn(true);

    try {
      await login(form.email.trim(), form.password);
      void navigate(from, { replace: true });
    } catch (caught) {
      setSubmitError(
        caught instanceof ApiError && caught.status === 401
          ? caught.message
          : toErrorMessage(caught, 'Could not sign in.'),
      );
      setIsSigningIn(false);
    }
  };

  return (
    <main className="login">
      <h1>Northwind Support</h1>

      <form className="card form" onSubmit={submit} noValidate aria-labelledby="login-heading">
        <h2 id="login-heading">Sign in</h2>

        <Field id="login-email" label="Email" error={errors.email}>
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="email"
              autoComplete="username"
              value={form.email}
              onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))}
            />
          )}
        </Field>

        <Field id="login-password" label="Password" error={errors.password}>
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="password"
              autoComplete="current-password"
              value={form.password}
              onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))}
            />
          )}
        </Field>

        {submitError && (
          <p className="field__error" role="alert">
            {submitError}
          </p>
        )}

        <div className="button-row">
          <button type="submit" className="button button--primary" disabled={isSigningIn}>
            {isSigningIn ? 'Signing in...' : 'Sign in'}
          </button>
        </div>
      </form>
    </main>
  );
}

function validate(form: LoginForm): Partial<Record<keyof LoginForm, string>> {
  const errors: Partial<Record<keyof LoginForm, string>> = {};

  if (form.email.trim() === '') {
    errors.email = 'Enter your email address.';
  } else if (!emailPattern.test(form.email.trim())) {
    errors.email = 'Enter a valid email address.';
  }

  if (form.password === '') {
    errors.password = 'Enter your password.';
  }

  return errors;
}
