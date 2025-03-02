# About
An AI powered twitch bot to serve the role of a twitch mod.

# config
The code will include a config.json file with it
The structure of the config.json goes as such
```json
{
  "access_token" : "access_token",
  "channel": "user_name",
  "guidelines" : "path_to_guideline_or_raw_guidelines",
  "spamming" : <integer_in_seconds>,
  "timeout" : <integer_in_minutes>,
  "banned" : <integer>
  "model" : "llm-model"
}
```

## access token
The access token is a key that you get from twitch to authorize the bots to do what you want

You can find a key at: https://twitchtokengenerator.com/

Pick bot token and copy the access token uptop

## channel
This is just your username for twitch nothing more. If you're using a different acount for the bot then this would be the username of the bot account

## guidelines
There are two ways of entering in guidelines for this program

### file path
    You can just enter in the file path of a txt for example that has the guidelines that your bot should follow
    You must ensure that the file path is the absolute file path rather than the relative file path

### raw text
    If you are familiar with how to edit json's you can simply just enter in the raw text into the quotation marks themselves

If you enter an empty string for whatever reason a generic set of community guidelines will pop up.

## spamming
This just holds an integer. It's how much someone needs to spread apart their messages to not be considered spamming
Saying 90 seconds for example will mean if someone texts 10 times in 90 seconds they will be called out for spamming

## timeout
This just holds an integer. It determines how long each timeout will last.

## banned
This just holds an integer. It determines how many timeouts will constitute a ban.

## model
The model will just be the large language model of your choosing.
The project restricts you to the use of ollama.
The best model I found was mistral