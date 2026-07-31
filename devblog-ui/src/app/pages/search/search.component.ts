import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PostService, PostSummary } from '../../services/post.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss'
})
export class SearchComponent implements OnInit {
  private postService = inject(PostService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  term = '';
  posts: PostSummary[] = [];
  currentPage = 1;
  pageSize = 20;
  totalPages = 0;
  totalCount = 0;
  searched = false;

  ngOnInit() {
    this.route.queryParamMap.subscribe((params) => {
      this.term = params.get('q') ?? '';
      const page = Number(params.get('page')) || 1;

      if (!this.term.trim()) {
        this.posts = [];
        this.totalPages = 0;
        this.totalCount = 0;
        this.searched = false;
        this.cdr.detectChanges();
        return;
      }

      this.loadResults(this.term, page);
    });
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }
    this.router.navigate([], { queryParams: { q: this.term, page } });
  }

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  toggleLike(post: PostSummary) {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.postService.toggleLike(post.slug).subscribe((result) => {
      post.likedByCurrentUser = result.liked;
      post.likeCount = result.likeCount;
      this.cdr.detectChanges();
    });
  }

  private loadResults(term: string, page: number) {
    this.postService.searchPosts(term, page, this.pageSize).subscribe((result) => {
      this.posts = result.items;
      this.currentPage = result.page;
      this.pageSize = result.pageSize;
      this.totalPages = result.totalPages;
      this.totalCount = result.totalCount;
      this.searched = true;
      this.cdr.detectChanges();
    });
  }
}
