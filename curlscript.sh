#!/bin/bash


pauseFlag=1
for arg in "$@"; do
    if [ "$arg" == "pause" ]; then
        pauseFlag=0
        break
    fi
done


if [ $pauseFlag -eq 1 ]; then read -p "Press enter to start..."; fi
echo -e "Register Player 1"

curl --location --request POST 'http://localhost:12000/users' \
--header 'Content-Type: application/json' \
--data-raw '{
    "username": "player1",
    "password": "pasword123"
}'

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi
echo -e "Login Player 1"

response=$(curl --location --request POST 'http://localhost:12000/sessions/' \
--header 'Content-Type: application/json' \
--data '{
    "username": "player1",
    "password": "pasword123"
}')

tokenPlayer1=$(echo $response | jq -r '.data.token')

echo "Token: $tokenPlayer1"


if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi
echo -e "Register Player 2"

curl --location --request POST 'http://localhost:12000/users' \
--header 'Content-Type: application/json' \
--data-raw '{
    "username": "player2",
    "password": "pasword123"
}'

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi
echo -e "Login Player 2"

response=$(curl --location --request POST 'http://localhost:12000/sessions/' \
--header 'Content-Type: application/json' \
--data '{
    "username": "player2",
    "password": "pasword123"
}')

tokenPlayer2=$(echo $response | jq -r '.data.token')

echo "Token: $tokenPlayer2"

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi
echo -e "Player 1 purchase package"

curl --location --request POST 'http://localhost:12000/purchase/0' \
--header "Authorization: Bearer $tokenPlayer1" \

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi
echo -e "Player 1 create trade offer"

curl --location --request POST 'http://localhost:12000/market' \
--header 'Content-Type: application/json' \
--header "Authorization: Bearer $tokenPlayer1" \
--data '{
    "offeredCardId": 36,
    "requestedCardId": 37
}'

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi
echo -e "Player 1 show trade offers"

response=$(curl --location --request GET 'http://localhost:12000/market' \
--header "Authorization: Bearer $tokenPlayer1")

echo $response

offerId=$(echo $response | jq -r '.offers[0].offerId')

echo "Offer ID: $offerId"

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi
echo -e "Player 2 purchase package"

curl --location --request POST 'http://localhost:12000/purchase/1' \
--header "Authorization: Bearer $tokenPlayer2" \

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi
echo -e "Player 2 accept trade offer"

curl --location --request PUT "http://localhost:12000/market/$offerId" \
--header "Authorization: Bearer $tokenPlayer2" \

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi
echo -e "Player 2 show cards"

curl --location --request GET 'http://localhost:12000/cards/me' \
--header "Authorization: Bearer $tokenPlayer2" \

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi

echo -e "Player 1 define deck"

curl --location --request PUT 'http://localhost:12000/deck' \
--header 'Content-Type: application/json' \
--header "Authorization: Bearer $tokenPlayer1" \
--data '{
    "cardIds": [37, 35, 34, 33]
}'

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi

echo -e "Player 2 define deck"
curl --location --request PUT 'http://localhost:12000/deck' \
--header 'Content-Type: application/json' \
--header "Authorization: Bearer $tokenPlayer2" \
--data '{
    "cardIds": [36, 39, 40, 41]
}'

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi

echo -e "Player 1 starts battle"

curl --location --request POST 'http://localhost:12000/battles' \
--header "Authorization: Bearer $tokenPlayer1" \

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi

echo -e "Player 2 starts battle"

curl --location --request POST 'http://localhost:12000/battles' \
--header "Authorization: Bearer $tokenPlayer2" \

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi

echo -e "Show Scoreboard"

curl --location --request GET 'http://localhost:12000/scoreboard' \



