#!/bin/sh

# --------------------------------------------------
# Monster Trading Cards Game
# --------------------------------------------------
echo "CURL Testing for Monster Trading Cards Game"
echo "Syntax: MonsterTradingCards.sh [-p]"
echo "- pause: optional, if set, then script will pause after each block"
echo .

pauseFlag=0
for arg in "$@"; do
    if [ "$arg" == "-p" ]; then
        pauseFlag=1
        break
    fi
done

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi

# --------------------------------------------------
echo "1) Create Users (Registration)"
# Create Users
curl -i -X POST http://localhost:12000/users --header "Content-Type: application/json" -d "{\"username\":\"kienboec\", \"password\":\"daniel\", \"fullname\":\"Clemens Kienboc\", \"email\":\"Clemens.Kienboc@gmail.com\"}"
echo "Should return HTTP 200"
echo .
curl -i -X POST http://localhost:12000/users --header "Content-Type: application/json" -d "{\"username\":\"altenhof\", \"password\":\"markus\", \"fullname\":\"Markus Altenhof\", \"email\":\"markus.altenhof@gmail.com\"}"
echo "Should return HTTP 200"
echo .
curl -i -X POST http://localhost:12000/users --header "Content-Type: application/json" -d "{\"username\":\"admin\", \"password\":\"istrator\", \"fullname\":\"Admin\", \"email\":\"admin@mtcg.com\"}"
echo "Should return HTTP 200"
echo .

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi

echo "should fail:"
curl -i -X POST http://localhost:12000/users --header "Content-Type: application/json" -d "{\"username\":\"kienboec\", \"password\":\"daniel\"}"
echo "Should return HTTP 4xx - User already exists"
echo .
curl -i -X POST http://localhost:12000/users --header "Content-Type: application/json" -d "{\"username\":\"kienboec\", \"password\":\"different\"}"
echo "Should return HTTP 4xx - User already exists"
echo . 
echo .

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi

# --------------------------------------------------
echo "2) Login Users"
curl -i -X POST http://localhost:12000/sessions --header "Content-Type: application/json" -d "{\"username\":\"kienboec\", \"password\":\"daniel\"}"
echo "should return HTTP 200 with generated token for the user"
echo .
curl -i -X POST http://localhost:12000/sessions --header "Content-Type: application/json" -d "{\"username\":\"altenhof\", \"password\":\"markus\"}"
echo "should return HTTP 200 with generated token for the user"
echo .
curl -i -X POST http://localhost:12000/sessions --header "Content-Type: application/json" -d "{\"username\":\"admin\",    \"password\":\"istrator\"}"
echo "should return HTTP 200 with generated token for the user"
echo .

if [ $pauseFlag -eq 1 ]; then read -p "Press enter to continue..."; fi

echo "should fail:"
curl -i -X POST http://localhost:12000/sessions --header "Content-Type: application/json" -d "{\"username\":\"kienboec\", \"password\":\"different\"}"
echo "Should return HTTP 4xx - Login failed"
echo .
echo .

echo "END"
