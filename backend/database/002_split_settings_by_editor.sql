alter table analyses
  add column if not exists lightroom_settings jsonb not null default '[]'::jsonb,
  add column if not exists darktable_settings jsonb not null default '[]'::jsonb;

alter table analyses
  alter column lightroom_settings drop default,
  alter column darktable_settings drop default;
