START TRANSACTION;

CREATE TABLE IF NOT EXISTS contributor
(
    id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    first_name VARCHAR(50),
    last_name  VARCHAR(50),
    full_name  VARCHAR(100)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_contributor_full_name ON contributor (full_name);

CREATE TABLE IF NOT EXISTS contributor_type
(
    id                           BIGINT PRIMARY KEY,
    contributor_type_description VARCHAR(50)
);


CREATE UNIQUE INDEX IF NOT EXISTS idx_contributor_type_contributor_type_description ON contributor_type (contributor_type_description
    );


CREATE TABLE IF NOT EXISTS contributor_contributor_type
(
    id               BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    contributor_type BIGINT,
    contributor_id   BIGINT,
    FOREIGN KEY (contributor_type) REFERENCES contributor_type (id),
    FOREIGN KEY (contributor_id) REFERENCES contributor (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_contributor_contributor_type ON contributor_contributor_type (contributor_id, contributor_type);

CREATE TABLE IF NOT EXISTS song
(
    id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        VARCHAR(50),
    path        VARCHAR(400),
    doc_date    TIMESTAMP,
    word_length INT
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_song_name ON song (name);


CREATE TABLE IF NOT EXISTS song_composers
(
    id               BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    contributor_id   BIGINT,
    contributor_type BIGINT,
    song_id          BIGINT,
    FOREIGN KEY (contributor_id) REFERENCES contributor (id),
    FOREIGN KEY (contributor_type) REFERENCES contributor_type (id),
    FOREIGN KEY (song_id) REFERENCES song (id)
);

CREATE INDEX IF NOT EXISTS idx_song_composers_contributor_song ON song_composers (contributor_id, contributor_type, song_id);


CREATE TABLE IF NOT EXISTS song_stanza
(
    id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    song_id     BIGINT,
    "offset"    INT,
    word_length INT,
    FOREIGN KEY (song_id) REFERENCES song (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_song_stanza_song_offset ON song_stanza (song_id, "offset");


CREATE TABLE IF NOT EXISTS song_line
(
    id             BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    song_id        BIGINT,
    "offset"       INT,
    word_length    INT,
    song_stanza_id BIGINT,
    FOREIGN KEY (song_id) REFERENCES song (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_song_line_song_offset ON song_line (song_id, "offset");

CREATE TABLE IF NOT EXISTS word
(
    id     BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    word   VARCHAR(45),
    length INT
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_word_word ON word (word);


CREATE TABLE IF NOT EXISTS song_word
(
    id                 BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    word_id            BIGINT,
    song_id            BIGINT,
    num_of_occurrences INT,
    FOREIGN KEY (word_id) REFERENCES word (id),
    FOREIGN KEY (song_id) REFERENCES song (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_song_word_song_word_id ON song_word (song_id, word_id);

CREATE TABLE IF NOT EXISTS word_location
(
    id           BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "offset"     INT,
    song_word_id BIGINT,
    song_line_id BIGINT,
    FOREIGN KEY (song_word_id) REFERENCES song_word (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_word_location_song_word_offset ON word_location (song_word_id, "offset");


CREATE TABLE IF NOT EXISTS "group"
(
    id   BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(50)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_group_name ON "group" (name);


CREATE TABLE IF NOT EXISTS word_group
(
    id       BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    group_id BIGINT,
    word_id  BIGINT,
    FOREIGN KEY (group_id) REFERENCES "group" (id),
    FOREIGN KEY (word_id) REFERENCES word (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_word_group_group_word ON word_group (group_id, word_id);


CREATE TABLE IF NOT EXISTS phrase
(
    id     BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    phrase_hash VARCHAR(250)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_phrase_phrase ON phrase (phrase_hash);


CREATE TABLE IF NOT EXISTS phrase_word
(
    id        BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    phrase_id BIGINT,
    word      VARCHAR(45),
    "offset"  INT,
    FOREIGN KEY (phrase_id) REFERENCES phrase (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_phrase_word_phrase_word_offset ON phrase_word (phrase_id, word, "offset");


INSERT INTO contributor_type (id, contributor_type_description)
VALUES (1, 'writer'),
       (2, 'music composer'),
       (3, 'performer')
ON CONFLICT DO NOTHING;

CREATE OR REPLACE VIEW phrase_view AS
SELECT p.id                     as phrase_id,
       STRING_AGG(pw.word, ', ') AS phrase_values
FROM phrase p
         JOIN phrase_word pw ON pw.phrase_id = p.id
group by p.id
order by p.id
;

CREATE OR REPLACE VIEW groups_view AS
SELECT g.id                     as group_id,
       g.name                   as group_name,
       STRING_AGG(w.word, ', ') AS group_values
FROM "group" g
         JOIN word_group wg ON wg.group_id = g.id
         JOIN word w ON wg.word_id = w.id
group by g.id, g.name
order by g.id
;

CREATE OR REPLACE VIEW word_index_view AS
SELECT w.word                AS word_text,
       wl."offset"           AS word_location_offset,
       sl."offset"           AS song_line_offset,
       ss."offset"           AS song_stanza_offset,
       sl.word_length        AS song_line_word_length,
       ss.word_length        AS song_stanza_word_length,
       sw.num_of_occurrences AS word_num_of_occurrences,
       w.id                  AS word_id,
       w.length              AS word_length,
       wl.id                 AS id,
       s.id                  as song_id
FROM word_location AS wl
         JOIN song_word AS sw ON sw.id = wl.song_word_id
         JOIN song AS s ON sw.song_id = s.id
         JOIN word AS w ON w.id = sw.word_id
         JOIN song_line AS sl ON wl.song_line_id = sl.id
         JOIN song_stanza AS ss ON sl.song_stanza_id = ss.id
ORDER BY wl.id;


CREATE OR REPLACE VIEW group_word_index_view AS
SELECT w.word                AS word_text,
       wl."offset"           AS word_location_offset,
       sl."offset"           AS song_line_offset,
       ss."offset"           AS song_stanza_offset,
       sl.word_length        AS song_line_word_length,
       ss.word_length        AS song_stanza_word_length,
       sw.num_of_occurrences AS word_num_of_occurrences,
       w.id                  AS word_id,
       w.length              AS word_length,
       wl.id                 AS id,
       s.id                  as song_id,
       g.name                as group_name
FROM word_location AS wl
         JOIN song_word AS sw ON sw.id = wl.song_word_id
         JOIN song AS s ON sw.song_id = s.id
         JOIN word AS w ON w.id = sw.word_id
         JOIN song_line AS sl ON wl.song_line_id = sl.id
         JOIN song_stanza AS ss ON sl.song_stanza_id = ss.id
         JOIN word_group wg on w.id = wg.word_id
         JOIN "group" as g on g.id = wg.group_id
ORDER BY wl.id;

CREATE OR REPLACE VIEW words_view AS
SELECT w.id                       AS word_id,
       w.word                     AS word_text,
       w.length                   AS word_length,
       SUM(sw.num_of_occurrences) AS sum_of_occurrences,
       MIN(wl."offset")           AS min_offset,
       s.id                       as song_id,
       s.name                     AS song_name
FROM song AS s
         JOIN song_word AS sw ON sw.song_id = s.id
         JOIN word AS w ON w.id = sw.word_id
         JOIN word_location AS wl ON sw.id = wl.song_word_id
GROUP BY s.id, s.name, w.id, w.word, w.length
ORDER BY MIN(wl."offset");

CREATE OR REPLACE VIEW songs_view AS
SELECT s.name                                                             as song_name,
       COALESCE(c.first_name, CAST('' AS VARCHAR(50)))                    as first_name,
       COALESCE(c.last_name, CAST('' AS VARCHAR(50)))                     as last_name,
       COALESCE(ct.contributor_type_description, CAST('' AS VARCHAR(50))) as contribution_type,
       s.path                                                             as file_path,
       s.doc_date                                                         as document_date,
       s.word_length                                                      as song_word_length,
       COALESCE(w.word, CAST('' AS VARCHAR(45)))                          as word,
       COALESCE(sc.id, 0)                                                 as song_composer_id
FROM song AS s
         LEFT JOIN song_composers AS sc ON s.id = sc.song_id
         LEFT JOIN contributor_type AS ct ON sc.contributor_type = ct.id
         LEFT JOIN contributor_contributor_type AS cct ON cct.contributor_id = ct.id
         LEFT JOIN contributor AS c ON c.id = sc.contributor_id
         LEFT JOIN song_word sw on s.id = sw.song_id
         LEFT JOIN word_location wl on sw.id = wl.song_word_id
         LEFT JOIN word w on sw.word_id = w.id
;

CREATE EXTENSION IF NOT EXISTS pg_trgm;


CREATE OR REPLACE FUNCTION search_song_by_similarity(term VARCHAR(50))
RETURNS TABLE (name VARCHAR(50))
AS $$
BEGIN
    RETURN QUERY
    SELECT s.name
    FROM song as s
    WHERE SIMILARITY(s.name, term) > 0.2
    ORDER BY SIMILARITY(s.name, term) DESC 
    LIMIT 10;
END;
$$ LANGUAGE plpgsql;


CREATE INDEX IF NOT EXISTS idx_contributor_first_name on contributor (first_name);
CREATE INDEX IF NOT EXISTS idx_contributor_last_name on contributor (last_name);
CREATE UNIQUE INDEX IF NOT EXISTS idx_contributor_type_contributor_type_description on contributor_type (contributor_type_description);


CREATE INDEX IF NOT EXISTS idx_contributor_contributor_id on contributor_contributor_type (contributor_id);
CREATE INDEX IF NOT EXISTS idx_contributor_contributor_type on contributor_contributor_type (contributor_type);


CREATE INDEX IF NOT EXISTS idx_song_name_trgm ON song USING gin (name gin_trgm_ops);


CREATE INDEX IF NOT EXISTS idx_song_composers_song_id ON song_composers (song_id);
CREATE INDEX IF NOT EXISTS idx_song_composers_contributor_id ON song_composers (contributor_id);
CREATE INDEX IF NOT EXISTS idx_song_composers_contributor_type ON song_composers (contributor_type);

CREATE INDEX IF NOT EXISTS idx_song_stanza_song_id_offset ON song_stanza (song_id, "offset");

CREATE INDEX IF NOT EXISTS idx_song_line_song_stanza_id ON song_line (song_stanza_id, "offset");

CREATE INDEX IF NOT EXISTS idx_song_word_song_id ON song_word (song_id);
CREATE INDEX IF NOT EXISTS idx_song_word_word_id ON song_word (word_id);
CREATE INDEX IF NOT EXISTS idx_song_word_song_id_word_id ON song_word (song_id, word_id);

CREATE INDEX IF NOT EXISTS idx_word_location_song_line_id_song_word_id ON word_location (song_line_id, song_word_id, "offset");


CREATE INDEX IF NOT EXISTS idx_word_group_word_id ON word_group (word_id);
CREATE INDEX IF NOT EXISTS idx_word_group_group_id ON word_group (group_id);

commit;

