import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { IconComponent } from '../../shared/ui/icon/icon.component';

interface Stat {
  value: string;
  label: string;
}

interface Step {
  title: string;
  body: string;
}

interface Feature {
  title: string;
  body: string;
  image: string;
  alt: string;
}

interface Testimonial {
  quote: string;
  name: string;
  lang: string;
}

@Component({
  selector: 'app-landing',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss',
})
export class LandingComponent {
  protected readonly stats: readonly Stat[] = [
    { value: '12k+', label: 'Learners' },
    { value: '40+', label: 'Languages' },
    { value: '3k+', label: 'Rooms this week' },
    { value: '24/7', label: 'Always open' },
  ];

  protected readonly steps: readonly Step[] = [
    {
      title: 'Find a room',
      body: 'Pick a voice, video, or text room near your level and drop in. No audience, no pressure.',
    },
    {
      title: 'Join the practice',
      body: 'Listen, type, or speak. Lurking is welcome, so jump in whenever you feel ready.',
    },
    {
      title: 'Come back tomorrow',
      body: 'A quiet feed and gentle XP keep you returning, without the streak guilt.',
    },
  ];

  protected readonly features: readonly Feature[] = [
    {
      title: 'A feed that is actually useful',
      body: 'Posts, prompts, and vocab from learners you follow. Practice you can read, reply to, and reuse.',
      image: '/images/landing-feed.png',
      alt: 'The LinguaSpace feed: posts from learners you follow, with replies and reactions.',
    },
    {
      title: 'Progress that stays quiet',
      body: 'XP and streaks track your effort, but they never shout. Recognition, not spectacle.',
      image: '/images/landing-leaderboard.png',
      alt: 'The LinguaSpace leaderboard: a calm ranking with your level and streak.',
    },
  ];

  protected readonly testimonial: Testimonial = {
    quote:
      'Finally a place where silence in a room is not awkward. No one is competing. We are just practising together.',
    name: 'Aya',
    lang: 'Japanese learner, level B2',
  };
}
