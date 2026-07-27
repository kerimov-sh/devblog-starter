import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PostService, PostSummary } from '../../services/post.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-post-list',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './post-list.component.html',
  styleUrl: './post-list.component.scss'
})
export class PostListComponent implements OnInit {
  private postService = inject(PostService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  posts: PostSummary[] = [];
  currentPage = 1;
  pageSize = 20;
  totalPages = 0;
  totalCount = 0;

  ngOnInit() {
    this.route.queryParamMap.subscribe((params) => {
      const page = Number(params.get('page')) || 1;
      this.loadPosts(page);
    });
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }
    this.router.navigate([], { queryParams: { page } });
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

  private loadPosts(page: number) {
    this.postService.getPosts(page, this.pageSize).subscribe((result) => {
      this.posts = result.items;
      this.currentPage = result.page;
      this.pageSize = result.pageSize;
      this.totalPages = result.totalPages;
      this.totalCount = result.totalCount;
      this.cdr.detectChanges(); //bu satır, değişiklikleri algılamak ve bileşeni güncellemek için ChangeDetectorRef kullanır
    });
  }
}
