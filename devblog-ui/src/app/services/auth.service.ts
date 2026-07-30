import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpInterceptorFn, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface AuthUser {
  username: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  // Backend artık JWT'yi httpOnly cookie olarak set ediyor (JS okuyamaz),
  // bu yüzden login state'i burada in-memory tutulur. Backend bir
  // "GET /auth/me" (session/whoami) endpoint'i sunmadığı için bu state
  // sayfa yenilendiğinde (F5) kaybolur; kullanıcının cookie'si hala
  // geçerli olsa bile isLoggedIn() false döner. Bu bilinen bir sınırlamadır
  // ve giderilmesi backend'e yeni bir endpoint eklenmesini gerektirir
  // (bu görevin kapsamı dışında).
  private currentUserSubject = new BehaviorSubject<AuthUser | null>(null);
  readonly currentUser$ = this.currentUserSubject.asObservable();

  login(username: string, password: string) {
    return this.http
      .post<AuthUser>(`${environment.apiUrl}/auth/login`, { username, password })
      .pipe(
        tap(user => {
          this.currentUserSubject.next(user);
        })
      );
  }

  logout() {
    // /auth/logout state-changing bir istek olduğu için CSRF header'ı
    // gerektiriyor; authInterceptor bunu otomatik ekler.
    return this.http.post(`${environment.apiUrl}/auth/logout`, {}).pipe(
      tap(() => this.currentUserSubject.next(null))
    );
  }

  isLoggedIn(): boolean {
    return this.currentUserSubject.value !== null;
  }
}

function readCookie(name: string): string | null {
  const match = document.cookie.match(new RegExp('(?:^|; )' + name + '=([^;]*)'));
  return match ? decodeURIComponent(match[1]) : null;
}

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  // Cookie'lerin (httpOnly access_token + XSRF-TOKEN) tarayıcı tarafından
  // gönderilip kabul edilebilmesi için her istekte credentials gerekli.
  let clonedReq = req.clone({ withCredentials: true });

  const method = req.method.toUpperCase();
  const isStateChanging = method === 'POST' || method === 'PUT' || method === 'DELETE' || method === 'PATCH';
  const isLogin = req.url.endsWith('/auth/login');

  if (isStateChanging && !isLogin) {
    const csrfToken = readCookie('XSRF-TOKEN');
    if (csrfToken) {
      clonedReq = clonedReq.clone({ setHeaders: { 'X-CSRF-Token': csrfToken } });
    }
  }

  return next(clonedReq);
};
