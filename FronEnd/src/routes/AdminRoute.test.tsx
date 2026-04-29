/* @vitest-environment jsdom */

import { render, screen, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AuthProvider } from "@/features/auth/context/AuthContext";
import { AdminRoute } from "./AdminRoute";

const AUTH_SESSION_KEY = "lootera_auth_session";

const renderAdminRoute = () => render(
  <AuthProvider>
    <MemoryRouter initialEntries={["/admin"]}>
      <Routes>
        <Route path="/" element={<div>Home</div>} />
        <Route
          path="/admin"
          element={(
            <AdminRoute>
              <div>Admin Panel</div>
            </AdminRoute>
          )}
        />
        <Route path="/login" element={<div>Login</div>} />
      </Routes>
    </MemoryRouter>
  </AuthProvider>,
);

describe("AdminRoute", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    window.localStorage.clear();
  });

  it("does not render admin panel from a tampered localStorage role", async () => {
    window.localStorage.setItem(
      AUTH_SESSION_KEY,
      JSON.stringify({
        accessToken: "customer-token",
        refreshToken: "refresh-token",
        accessTokenExpiresAtUtc: "2099-01-01T00:00:00.000Z",
        refreshTokenExpiresAtUtc: "2099-01-02T00:00:00.000Z",
        user: {
          id: "customer-id",
          name: "Cliente",
          email: "cliente@example.com",
          role: "admin",
        },
      }),
    );

    vi.stubGlobal("fetch", vi.fn(async () => new Response(JSON.stringify({
      id: "customer-id",
      name: "Cliente",
      email: "cliente@example.com",
      role: "Customer",
      emailVerified: true,
      createdAtUtc: "2026-01-01T00:00:00.000Z",
    }), { status: 200 })));

    renderAdminRoute();

    await waitFor(() => expect(screen.getByText("Home")).toBeInTheDocument());
    expect(screen.queryByText("Admin Panel")).not.toBeInTheDocument();
    expect(window.localStorage.getItem(AUTH_SESSION_KEY)).toBeNull();
  });
});
