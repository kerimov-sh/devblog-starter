import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <nav class="navbar navbar-expand-lg navbar-light bg-light mb-4">
      <div class="container">
        <span class="navbar-brand">DevBlog</span>
        <div class="navbar-nav">
          <a class="nav-link" routerLink="/posts">Posts</a>
          <a class="nav-link" routerLink="/login">Login</a>
        </div>
      </div>
    </nav>
    <main>
      <router-outlet />
    </main>
  `
})
export class AppComponent {}
