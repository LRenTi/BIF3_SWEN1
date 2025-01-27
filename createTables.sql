CREATE TABLE users (
    username VARCHAR(100) PRIMARY KEY,
    password VARCHAR(255) NOT NULL,
    coins INTEGER DEFAULT 20,
    elo INTEGER DEFAULT 100,
    wins INTEGER DEFAULT 0,
    losses INTEGER DEFAULT 0,
    games INTEGER DEFAULT 0,
    is_admin BOOLEAN DEFAULT FALSE
);

CREATE TABLE cards (
    card_id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    damage INT NOT NULL,
    element VARCHAR(50) NOT NULL,
    card_type VARCHAR(50) NOT NULL
); 

CREATE TABLE user_cards (
    user_card_id SERIAL PRIMARY KEY,
    username VARCHAR(255) NOT NULL,
    card_id INT NOT NULL,
    is_in_deck BOOLEAN NOT NULL,
    FOREIGN KEY (card_id) REFERENCES cards(card_id)
); 

CREATE TABLE packages (
    package_id INTEGER PRIMARY KEY,
    price INTEGER NOT NULL
);

CREATE TABLE package_cards (
    package_card_id SERIAL PRIMARY KEY,
    package_id INT NOT NULL,
    card_id INT NOT NULL,
    FOREIGN KEY (package_id) REFERENCES packages(package_id),
    FOREIGN KEY (card_id) REFERENCES cards(card_id)
); 

CREATE TABLE trade_offers (
    offer_id SERIAL PRIMARY KEY,
    username VARCHAR(255) NOT NULL,
    offered_card_id INT NOT NULL,
    requested_card_id INT NOT NULL,
    FOREIGN KEY (offered_card_id) REFERENCES cards(card_id),
    FOREIGN KEY (requested_card_id) REFERENCES cards(card_id)
); 