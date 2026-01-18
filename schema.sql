BEGIN;

CREATE TABLE IF NOT EXISTS users (
  id            SERIAL PRIMARY KEY,
  username      VARCHAR(50)  NOT NULL UNIQUE,
  password_hash VARCHAR(200) NOT NULL,
  display_name  VARCHAR(100),
  created_at    TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT now(),
  bio           TEXT,
  updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS media (
  id              SERIAL PRIMARY KEY,
  title           VARCHAR(200) NOT NULL,
  description     TEXT,
  media_type      VARCHAR(20)  NOT NULL,
  release_year    INTEGER,
  genre           VARCHAR(100),
  age_restriction INTEGER,
  created_by      INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  created_at      TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT now(),
  updated_at      TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_media_title ON media(title);

CREATE TABLE IF NOT EXISTS sessions (
  token      TEXT PRIMARY KEY,
  user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  expires_at TIMESTAMPTZ NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_sessions_user_id    ON sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_sessions_expires_at ON sessions(expires_at);

CREATE TABLE IF NOT EXISTS ratings (
  id                SERIAL PRIMARY KEY,
  user_id           INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  media_id          INTEGER NOT NULL REFERENCES media(id) ON DELETE CASCADE,
  stars             INTEGER NOT NULL,
  comment           TEXT,
  comment_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
  created_at        TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT now(),
  updated_at        TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT now(),
  CONSTRAINT ratings_stars_range CHECK (stars >= 1 AND stars <= 5),
  CONSTRAINT ratings_unique_user_media UNIQUE (user_id, media_id)
);

CREATE INDEX IF NOT EXISTS idx_ratings_user_id  ON ratings(user_id);
CREATE INDEX IF NOT EXISTS idx_ratings_media_id ON ratings(media_id);

CREATE TABLE IF NOT EXISTS rating_likes (
  user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  rating_id  INTEGER NOT NULL REFERENCES ratings(id) ON DELETE CASCADE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, rating_id)
);

CREATE INDEX IF NOT EXISTS idx_rating_likes_user_id   ON rating_likes(user_id);
CREATE INDEX IF NOT EXISTS idx_rating_likes_rating_id ON rating_likes(rating_id);

CREATE TABLE IF NOT EXISTS favorites (
  user_id  INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  media_id INTEGER NOT NULL REFERENCES media(id) ON DELETE CASCADE,
  PRIMARY KEY (user_id, media_id)
);

CREATE INDEX IF NOT EXISTS idx_favorites_user_id  ON favorites(user_id);
CREATE INDEX IF NOT EXISTS idx_favorites_media_id ON favorites(media_id);

COMMIT;
