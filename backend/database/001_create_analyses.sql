create table if not exists analyses (
  id uuid primary key,
  file_name text not null,
  created_at timestamptz not null,
  summary text not null,
  lightroom_settings jsonb not null,
  darktable_settings jsonb not null
);

create index if not exists idx_analyses_created_at on analyses (created_at desc);
