import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { Observable } from "rxjs";
import LoginDto from "../DTOs/LoginDTO";
import RegiterDTO from "../DTOs/RegisterDTO";



@Injectable({ providedIn: "root" })
export class AuthService {

  private api = "/api/auth/";
  private tokenKey = "monitex_token";
  private userStateKey = "monitex_user_state";

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  /*
  =========================
  REGISTER
  =========================
  */

  register(data: RegiterDTO): Observable<any> {
    return this.http.post(
      this.api + "register",
      data
    );
  }

  /*
  =========================
  LOGIN
  =========================
  */

  login(data: LoginDto): Observable<any> {
    return this.http.post(
      this.api + "login",
      data
    );
  }

  handleLoginSuccess(response: any): void {
    const token = this.extractToken(response);

    if (token) {
      this.saveToken(token);
    }

    // Prefer JWT claims (hasDevices / hasSensors) over a stale local cache.
    const state = this.extractUserState(response, token ?? this.getToken());
    this.setOnboardingFlags(state.hasDevices, state.hasSensors);
    void this.router.navigateByUrl(this.resolvePostLoginRoute());
  }

  /**
   * New accounts always start onboarding at add-device.
   */
  handleRegisterSuccess(response: any): void {
    const token = this.extractToken(response);

    if (token) {
      this.saveToken(token);
    }

    this.setOnboardingFlags(false, false);
    void this.router.navigateByUrl('/add-device');
  }

  /*
  =========================
  SAVE TOKEN
  =========================
  */

  saveToken(token: string): void {
    localStorage.setItem(
      this.tokenKey,
      token
    );
  }

  /*
  =========================
  GET TOKEN
  =========================
  */

  getToken(): string | null {
    return localStorage.getItem(
      this.tokenKey
    );
  }

  /*
  =========================
  LOGOUT
  =========================
  */

  logout(): void {

    localStorage.removeItem(
      this.tokenKey
    );
    localStorage.removeItem(
      this.userStateKey
    );

    this.router.navigate(
      ["/login"]
    );
  }

  /*
  =========================
  CHECK IF USER LOGGED IN
  =========================
  */

  isLoggedIn(): boolean {

    const token =
      this.getToken();

    if (!token)
      return false;

    return !this.isTokenExpired(token);
  }

  /*
  =========================
  CHECK TOKEN EXPIRATION
  =========================
  */

  private isTokenExpired(
    token: string
  ): boolean {
    const payload = this.decodeTokenPayload(token);

    if (!payload?.exp) {
      return true;
    }

    return payload.exp * 1000 < Date.now();
  }

  /*
  =========================
  GET USER ID FROM TOKEN
  =========================
  */

  getUserId(): number | null {
    const token = this.getToken();

    if (!token) {
      return null;
    }

    const payload = this.decodeTokenPayload(token);

    if (!payload) {
      return null;
    }

    const rawId =
      payload.userId ??
      payload.nameid ??
      payload.sub ??
      payload[
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
      ];

    const parsed = Number(rawId);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }

  /*
  =========================
  AUTO LOGIN REDIRECT
  =========================
  */

  redirectIfLoggedIn(): void {
    if (this.isLoggedIn()) {
      void this.router.navigateByUrl(this.resolvePostLoginRoute());
    }
  }

  /**
   * Post auth routing:
   * - no devices → /add-device
   * - devices but no sensors → /add-sensors
   * - both → /dashboard
   */
  resolvePostLoginRoute(): string {
    const state = this.getOnboardingState();

    if (!state.hasDevices) {
      return '/add-device';
    }

    if (!state.hasSensors) {
      return '/add-sensors';
    }

    return '/dashboard';
  }

  continueOnboardingAfterDevice(): void {
    this.markHasDevices();
    void this.router.navigateByUrl('/add-sensors');
  }

  continueOnboardingAfterSensors(): void {
    this.setOnboardingFlags(true, true);
    void this.router.navigateByUrl('/dashboard');
  }

  getOnboardingState(): { hasDevices: boolean; hasSensors: boolean } {
    return this.getStoredUserState();
  }

  setOnboardingFlags(hasDevices: boolean, hasSensors: boolean): void {
    localStorage.setItem(
      this.userStateKey,
      JSON.stringify({ hasDevices, hasSensors })
    );
  }

  markHasDevices(): void {
    const state = this.getOnboardingState();
    this.setOnboardingFlags(true, state.hasSensors);
  }

  markHasSensors(): void {
    const state = this.getOnboardingState();
    this.setOnboardingFlags(state.hasDevices, true);
  }

  private extractToken(response: any): string | null {
    const possibleToken =
      response?.token ??
      response?.accessToken ??
      response?.jwt ??
      response?.data?.token ??
      response?.data?.accessToken;

    return typeof possibleToken === "string" && possibleToken.trim()
      ? possibleToken
      : null;
  }

  private getStoredUserState(): { hasDevices: boolean; hasSensors: boolean } {
    const rawState = localStorage.getItem(this.userStateKey);

    if (rawState) {
      try {
        return JSON.parse(rawState);
      } catch {
        localStorage.removeItem(this.userStateKey);
      }
    }

    return this.extractUserState(null, this.getToken());
  }

  private extractUserState(
    response: any,
    token: string | null
  ): { hasDevices: boolean; hasSensors: boolean } {
    const payload = token ? this.decodeTokenPayload(token) : null;
    const sources = this.resolveUserStateSources(response, payload);

    const hasDevices = this.pickBoolean(sources, [
      "hasDevices",
      "hasDevice",
      "devicesExist",
      "deviceExists",
    ]) ?? this.pickCount(sources, [
      "deviceCount",
      "devicesCount",
      "totalDevices",
      "devices",
    ]) > 0;

    const hasSensors = this.pickBoolean(sources, [
      "hasSensors",
      "hasSensor",
      "sensorsExist",
      "sensorExists",
    ]) ?? this.pickCount(sources, [
      "sensorCount",
      "sensorsCount",
      "totalSensors",
      "sensors",
    ]) > 0;

    return { hasDevices, hasSensors };
  }

  private pickBoolean(
    sources: any[],
    keys: string[]
  ): boolean | null {
    for (const source of sources) {
      if (!source || typeof source !== "object") {
        continue;
      }

      for (const key of keys) {
        const value = source[key];

        if (typeof value === "boolean") {
          return value;
        }

        if (typeof value === "string") {
          const normalizedValue = value.trim().toLowerCase();

          if (normalizedValue === "true") {
            return true;
          }

          if (normalizedValue === "false") {
            return false;
          }
        }
      }
    }

    return null;
  }

  private pickCount(
    sources: any[],
    keys: string[]
  ): number {
    for (const source of sources) {
      if (!source || typeof source !== "object") {
        continue;
      }

      for (const key of keys) {
        const value = source[key];

        if (typeof value === "number") {
          return value;
        }

        if (Array.isArray(value)) {
          return value.length;
        }
      }
    }

    return 0;
  }

  private resolveUserStateSources(
    response: any,
    payload: any
  ): any[] {
    // JWT claims are the source of truth from login/register.
    return [
      payload,
      response,
      response?.data,
      response?.user,
      response?.profile,
    ];
  }

  private decodeTokenPayload(token: string): any | null {
    try {
      const segment = token.split(".")[1];

      if (!segment) {
        return null;
      }

      const base64 = segment
        .replace(/-/g, "+")
        .replace(/_/g, "/");
      const padded = base64.padEnd(
        base64.length + ((4 - (base64.length % 4)) % 4),
        "="
      );

      return JSON.parse(atob(padded));
    } catch {
      return null;
    }
  }

}
