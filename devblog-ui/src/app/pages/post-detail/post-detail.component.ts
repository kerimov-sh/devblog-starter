import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PostService, PostDetail } from '../../services/post.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-post-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './post-detail.component.html'
})
export class PostDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);
  private postService = inject(PostService);
  private authService = inject(AuthService);
  private router = inject(Router);

  post: PostDetail | null = null;
  commentAuthor = '';
  commentBody = '';
  submitted = false;

  ngOnInit() {
    const slug = this.route.snapshot.paramMap.get('slug')!;
    this.postService.getPost(slug).subscribe(p => {
      this.post = p;
      this.cdr.detectChanges(); //bu satır, değişiklikleri algılamak ve bileşeni güncellemek için ChangeDetectorRef kullanır

    } );
  }

  toggleLike() {
    if (!this.post) return;
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.postService.toggleLike(this.post.slug).subscribe((result) => {
      if (!this.post) return;
      this.post.likedByCurrentUser = result.liked;
      this.post.likeCount = result.likeCount;
      this.cdr.detectChanges();
    });
  }

  submitComment() {
    if (!this.post) return;
    this.postService
      .addComment(this.post.slug, { authorName: this.commentAuthor, body: this.commentBody })
      .subscribe(() => {
        this.submitted = true;
        this.commentAuthor = '';
        this.commentBody = '';
        const slug = this.route.snapshot.paramMap.get('slug')!;
        this.postService.getPost(slug).subscribe(p => (this.post = p));
      });
  }
}
