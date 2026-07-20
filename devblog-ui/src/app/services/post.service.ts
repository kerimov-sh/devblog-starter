import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PostSummary {
  id: number;
  title: string;
  slug: string;
  tags: string;
  publishedAt: string;
  author: string;
}

export interface PostDetail extends PostSummary {
  content: string;
  comments: Comment[];
}

export interface Comment {
  id: number;
  authorName: string;
  body: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class PostService {
  private http = inject(HttpClient);

  getPosts(page = 1, pageSize = 20) {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<PostSummary>>(`${environment.apiUrl}/posts`, { params });
  }

  getPost(slug: string) {
    return this.http.get<PostDetail>(`${environment.apiUrl}/posts/${slug}`);
  }

  createPost(data: { title: string; content: string; slug: string; tags: string }) {
    return this.http.post(`${environment.apiUrl}/posts`, data);
  }

  addComment(slug: string, data: { authorName: string; body: string }) {
    return this.http.post(`${environment.apiUrl}/posts/${slug}/comments`, data);
  }
}
