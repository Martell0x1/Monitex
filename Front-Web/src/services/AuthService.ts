import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { Observable } from "rxjs";
import LoginDto from "../DTOs/LoginDTO";
import RegiterDTO from "../DTOs/RegisterDTO";



@Injectable({ providedIn: "root" })
export class AuthService {

  private api = "http://localhost:5020/api/auth/";
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

    this.saveUserState(response, token);
    this.router.navigate([this.resolvePostLoginRoute()]);
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

    try {

      const payload =
        JSON.parse(
          atob(token.split(".")[1])
        );

      return (
        payload.exp * 1000
        < Date.now()
      );

    } catch {

      return true;

    }
  }

  /*
  =========================
  GET USER ID FROM TOKEN
  =========================
  */

  getUserId(): number | null {

    const token =
      this.getToken();

    if (!token)
      return null;

    try {

      const payload =
        JSON.parse(
          atob(token.split(".")[1])
        );

      return payload.userId;

    } catch {

      return null;

    }
  }

  /*
  =========================
  AUTO LOGIN REDIRECT
  =========================
  */

  redirectIfLoggedIn(): void {

    if (this.isLoggedIn()) {

      this.router.navigate(
        [this.resolvePostLoginRoute()]
      );

    }

  }

  resolvePostLoginRoute(): string {
    const state = this.getStoredUserState();

    if (state.hasDevices && state.hasSensors) {
      return "/dashboard";
    }

    if (state.hasDevices) {
      return "/add-sensors";
    }

    return "/add-device";
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

  private saveUserState(response: any, token: string | null): void {
    const state = this.extractUserState(response, token);

    localStorage.setItem(
      this.userStateKey,
      JSON.stringify(state)
    );
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
    return [
      response,
      response?.data,
      response?.user,
      response?.profile,
      payload,
    ];
  }

  private decodeTokenPayload(token: string): any | null {
    try {
      return JSON.parse(
        atob(token.split(".")[1])
      );
    } catch {
      return null;
    }
  }

}
