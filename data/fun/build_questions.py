#!/usr/bin/env python3
"""Write data/fun/questions.json. The JSON file is the curated source."""

import json
from pathlib import Path

# (category, tag, likelihood, question, reasoning)
ROWS = []


def add(category, tag, likelihood, question, reasoning):
    ROWS.append(
        {
            "category": category,
            "tag": tag,
            "likelihood": likelihood,
            "question": question,
            "reasoning": reasoning,
        }
    )


# Pop culture
add(
    "Pop culture",
    "Base rate",
    82,
    "Will the next big superhero film include a post-credits scene?",
    "In a typical run of ten tentpole superhero films, about eight stick a short scene after the names. The habit is older than most of the audience. A quiet ending is the exception.",
)
add(
    "Pop culture",
    "Vibes",
    48,
    "Will a classic sitcom reunion trend for a full day?",
    "A familiar cast photo usually gets a warm afternoon, then the timeline moves on. Across ordinary weeks, about half of those reunions are still being shared the next morning. The rest fade before dinner.",
)
add(
    "Pop culture",
    "Base rate",
    57,
    "Will a video game slip from its announced season into the next one?",
    "A typical decade of announced games moves a little over half of the ambitious ones into the following season. Slippage is ordinary, not a scandal. The ones that land on time are the ones people remember as punctual.",
)
add(
    "Pop culture",
    "Vibes",
    41,
    "Will a red-carpet host mispronounce a name on live TV?",
    "Live carpets are loud and the cue cards are tiny. In a typical season of big broadcasts, a stumbled name shows up less than half the time. Most hosts recover before the next guest.",
)
add(
    "Pop culture",
    "Vibes",
    23,
    "Will a franchise announce a third film before the second one opens?",
    "Studios like a plan, but they usually wait for a weekend of ticket news. In a typical decade, an early third-film announcement happens about twice in ten cycles. Patience is the more common pose.",
)
add(
    "Pop culture",
    "Base rate",
    36,
    "Will a streaming service drop a surprise episode at midnight?",
    "Most episodes arrive when the schedule said they would. A typical year of big shows springs a midnight drop about a third of the time. The calendar is still the main character.",
)
add(
    "Pop culture",
    "Base rate",
    64,
    "Will a toy aisle restock a sold-out figure within a month?",
    "A sold-out peg is often a shipping pause, not a permanent exit. In a typical month of ordinary figures, about six in ten return before the next moon. The chase variants take longer.",
)
add(
    "Pop culture",
    "Base rate",
    71,
    "Will a comic convention panel run past its scheduled slot?",
    "Panels start with good intentions and a clock that nobody watches. Across a typical convention weekend, about seven in ten panels spill past the printed minute. The hallway crowd already expects it.",
)
add(
    "Pop culture",
    "Base rate",
    66,
    "Will a celebrity book club pick a novel that was already selling well?",
    "Book clubs love a discovery and also love a book people can find. In a typical year of picks, about two thirds were already moving before the announcement. The cold start is rarer than the bump.",
)
add(
    "Pop culture",
    "Base rate",
    88,
    "Will a new theme park ride show a 20 minute standby in opening week?",
    "Opening week is a queue with a ribbon on it. In a typical decade of new rides, nearly nine in ten post a 20 minute wait while the paint is still fresh. The empty morning comes later.",
)
add(
    "Pop culture",
    "Vibes",
    14,
    "Will a late-night joke still be quoted a year later?",
    "Most monologue jokes live for a night and a group chat. Across a typical year of bits, only a small handful are still quoted twelve months on. The archive is deep and the memory is short.",
)
add(
    "Pop culture",
    "Base rate",
    39,
    "Will a soundtrack single outlive the movie that introduced it?",
    "Some songs leave the film behind, and many stay in the credits. In a typical decade of soundtrack singles, about four in ten are still played after the movie has left theaters. The rest stay attached to the story.",
)
add(
    "Pop culture",
    "Vibes",
    77,
    "Will a detailed cosplayer at a big convention be asked for a photo within ten minutes?",
    "A careful costume is a walking hello. On a typical busy convention floor, about three quarters of those builds get a photo request inside ten minutes. The quiet corners are the exception.",
)
add(
    "Pop culture",
    "Vibes",
    58,
    "Will a trailer dropped on Thursday pass a million views by Friday?",
    "A Thursday drop has a night and a morning to travel. For a typical studio trailer with a known title, a little over half clear a million views before Friday night. Smaller films take the scenic route.",
)
add(
    "Pop culture",
    "Vibes",
    44,
    "Will a spinoff keep the original theme song?",
    "Lawyers, composers, and nostalgia all get a vote. In a typical run of spinoffs, the old theme survives a bit under half the time. A new sting is just as common.",
)
add(
    "Pop culture",
    "Vibes",
    19,
    "Will a blooper reel run longer than the scene it came from?",
    "Most gag reels are a minute of flubs, not a second movie. Across ordinary home-video extras, a blooper that outlasts its scene happens about twice in ten. The short laugh wins.",
)
add(
    "Pop culture",
    "Base rate",
    91,
    "Will a fandom wiki page get edited the same day a finale airs?",
    "Finales are homework for volunteers. In a typical decade of big endings, about nine in ten wiki pages move on the same day. Someone is always waiting with the plot section open.",
)
add(
    "Pop culture",
    "Vibes",
    53,
    "Will a limited popcorn bucket sell out before a movie's second weekend?",
    "A novelty bucket is half souvenir and half snack. Across a typical opening, about half are gone before the second weekend. The plain bucket stays.",
)

# Memes and internet
add(
    "Memes and internet",
    "Base rate",
    27,
    "Will a new dance trend still be posted a month later?",
    "Dance sounds travel fast and cool off faster. In a typical month of new moves, only about a quarter are still being posted four weeks on. The audio page tells the truth.",
)
add(
    "Memes and internet",
    "Vibes",
    73,
    "Will a group chat screenshot lose the joke once it is public?",
    "In-jokes need the room they were born in. Across ordinary leaks of a funny thread, about seven in ten land flatter in public than they did at home. Context was the punchline.",
)
add(
    "Memes and internet",
    "Base rate",
    96,
    "Will someone comment first under a video that already has a million views?",
    "A million-view video has already collected a crowd. In a typical case the early comments include at least one person announcing they are first, even when they are not. The bit refuses to die.",
)
add(
    "Memes and internet",
    "Vibes",
    11,
    "Will a busy comment section stay kind for a whole afternoon?",
    "A quiet thread can be decent. A busy one usually finds an argument before the afternoon is over. Across ordinary viral posts, a fully kind afternoon happens about one time in ten.",
)
add(
    "Memes and internet",
    "Base rate",
    68,
    "Will an old meme account post a format it already used last year?",
    "Formats are tools, and accounts reuse the ones that worked. In a typical year, about two thirds of veteran meme pages bring back a layout from the year before. Novelty is optional.",
)
add(
    "Memes and internet",
    "Vibes",
    33,
    "Will a loading spinner last longer than the clip it precedes?",
    "Most clips outlast the spinner. On a typical slow connection, about a third of short videos make you wait longer than the video itself. The spinner is the opening act nobody asked for.",
)
add(
    "Memes and internet",
    "Vibes",
    18,
    "Will a reply actually outscore the post it is answering?",
    "The original post has the head start. Across ordinary attempts to outscore it, the reply wins about two times in ten. The rest stay underneath, which is where replies live.",
)
add(
    "Memes and internet",
    "Base rate",
    62,
    "Will a Wikipedia session pass six articles in one sitting?",
    "One question becomes a blue link, then another. In a typical curious evening, about six in ten sessions pass six articles before the tab is closed. The first page was only the door.",
)
add(
    "Memes and internet",
    "Base rate",
    84,
    "Will a livestream chat repeat the same emote for a full minute?",
    "Chats love a chorus. On a typical lively stream, about eight in ten rooms hammer one emote for a minute at least once. The message is the repetition.",
)
add(
    "Memes and internet",
    "Vibes",
    93,
    "Will a day-in-my-life video skip the boring parts?",
    "Nobody films the full wait at the sink. Across ordinary day-in-the-life edits, more than nine in ten leave out the dull middle. The day was longer than the video.",
)
add(
    "Memes and internet",
    "Base rate",
    79,
    "Will a password reset email arrive in under two minutes?",
    "Reset mail is one of the few emails that still hurries. In a typical batch, about eight in ten land inside two minutes. The rest are sitting in a spam folder, pretending to be late.",
)
add(
    "Memes and internet",
    "Base rate",
    47,
    "Will a recommended video be one you already watched?",
    "Recommendation rows have a memory and a blind spot. Across ordinary evenings, a bit under half of the top suggestions are something already finished. The row is confident, and you were already there.",
)
add(
    "Memes and internet",
    "Base rate",
    55,
    "Will a forum thread from years ago still answer the question?",
    "Old threads age in public. In a typical search, a little over half of the ancient posts still contain the fix. The other half describe a button that moved.",
)
add(
    "Memes and internet",
    "Vibes",
    38,
    "Will someone caption a pet photo with the wrong song lyric?",
    "Pet photos attract confident captions. Across a typical week of them, a mismatched lyric shows up a bit under four times in ten. The pet remains unbothered.",
)
add(
    "Memes and internet",
    "Vibes",
    42,
    "Will a quick poll still be collecting votes the next day?",
    "Quick is a mood, not a timer. In a typical batch of casual polls, about four in ten are still open after a day. The rest were decided before lunch.",
)
add(
    "Memes and internet",
    "Base rate",
    86,
    "Will an unsubscribe link be smaller than the logo above it?",
    "Newsletters know which pixel pays. Across ordinary marketing mail, the unsubscribe line is smaller than the logo about eight or nine times in ten. The logo did not wander there by accident.",
)
add(
    "Memes and internet",
    "Vibes",
    29,
    "Will a viral recipe skip a step that matters?",
    "Most viral recipes are complete enough to eat. In a typical dozen, about three leave out a step you notice halfway through. The comments then become the real method.",
)
add(
    "Memes and internet",
    "Base rate",
    74,
    "Will the nobody-colon meme format show up again this month?",
    "That setup is a cockroach in a nice font. Across ordinary months on large platforms, it reappears about three weeks in four. Retirement keeps getting postponed.",
)

# Movies and TV
add(
    "Movies and TV",
    "Base rate",
    70,
    "Will a heist movie explain the plan twice?",
    "Heist plots like a diagram and a reminder. In a typical decade of them, about seven in ten walk through the plan a second time before the job. The audience is part of the crew.",
)
add(
    "Movies and TV",
    "Base rate",
    76,
    "Will the detective's hunch be right by the last act?",
    "Mysteries are allowed one wrong turn and a correct ending. Across ordinary detective films, the hunch is right by the last act about three quarters of the time. The red herrings are for the middle.",
)
add(
    "Movies and TV",
    "Vibes",
    34,
    "Will a horror film cut to a cat before the real scare?",
    "The fake-out cat is a classic and not a requirement. In a typical batch of horror films, about a third use the animal beat before the real one. The rest go straight for the door.",
)
add(
    "Movies and TV",
    "Base rate",
    45,
    "Will a season finale cliffhanger be softened in the next premiere?",
    "Cliffhangers promise chaos and premieres have to keep a show alive. Across ordinary serialized seasons, a bit under half walk the danger back in episode one. The rest pick up the pieces for real.",
)
add(
    "Movies and TV",
    "Base rate",
    81,
    "Will the credits on a blockbuster run past three minutes?",
    "Big films have big crews. In a typical decade of blockbusters, about eight in ten credit rolls pass three minutes. The songs during them are doing a job.",
)
add(
    "Movies and TV",
    "Vibes",
    63,
    "Will a time-travel plot contradict itself on a rewatch?",
    "Time travel has too many doors. Across ordinary films that use it, a careful second viewing finds a snag about six times in ten. The first viewing was having too much fun to mind.",
)
add(
    "Movies and TV",
    "Base rate",
    52,
    "Will a sitcom bottle episode stay inside the apartment?",
    "Bottle episodes save a set and a budget. In a typical long-running sitcom, about half of those episodes never leave the main room. The hallway still counts as travel.",
)
add(
    "Movies and TV",
    "Vibes",
    28,
    "Will the mentor survive a whole trilogy?",
    "Mentors have a dangerous job description. Across ordinary trilogies, the guide is still standing at the end about three times in ten. The speech usually lands before the exit.",
)
add(
    "Movies and TV",
    "Base rate",
    67,
    "Will a cooking-show timer beep during the final plate?",
    "The clock is a character. In a typical season of timed cooking shows, the beep arrives during the last plates about two thirds of the time. Somebody is always wiping a rim in a hurry.",
)
add(
    "Movies and TV",
    "Vibes",
    40,
    "Will a documentary leave a long pause in the interview?",
    "Some editors protect the silence and some fill it. Across ordinary interview docs, a long pause survives the cut about four times in ten. The rest get a polite trim.",
)
add(
    "Movies and TV",
    "Base rate",
    88,
    "Will a family cartoon hide a joke meant for adults in the background?",
    "Family animation has been doing this for decades. In a typical run of them, nearly nine in ten plant at least one background joke for the grownups. The kids are watching the dragon.",
)
add(
    "Movies and TV",
    "Vibes",
    21,
    "Will the previously-on recap spoil the cold open?",
    "Recaps are supposed to set the table, not eat the meal. Across ordinary premieres, the recap gives away the cold open about one time in five. Editors usually notice in time.",
)
add(
    "Movies and TV",
    "Base rate",
    59,
    "Will a courtroom drama bring in one surprise witness?",
    "Courtroom stories love a door opening late. In a typical decade of them, a surprise witness appears a bit over half the time. The objection is part of the rhythm.",
)
add(
    "Movies and TV",
    "Base rate",
    49,
    "Will a holiday special reuse last year's set?",
    "Holiday sets are expensive and already red. Across ordinary annual specials, about half reuse the previous set with new tinsel. The other half rebuild the same living room anyway.",
)
add(
    "Movies and TV",
    "Vibes",
    54,
    "Will a series finale bring back a character from the first season?",
    "Finales like a circle. In a typical long series, a bit over half invite a season-one face back for the ending. The rest trust the people who stayed.",
)
add(
    "Movies and TV",
    "Vibes",
    37,
    "Will a musical number start with a single piano note?",
    "The single note is a mood, not a rule. Across ordinary musical numbers, it opens the song a bit under four times in ten. Plenty of them start with a drum and no warning.",
)
add(
    "Movies and TV",
    "Vibes",
    83,
    "Will the monster be scarier before you see it clearly?",
    "Imagination has a bigger budget than latex. In a typical horror film, the unseen stretch is the scary one about eight times in ten. The full look is often a relief.",
)
add(
    "Movies and TV",
    "Base rate",
    46,
    "Will a pilot introduce more than eight named characters?",
    "Pilots have a lot to introduce and a short runtime. Across ordinary first episodes, a bit under half name more than eight people. The rest let you learn two faces at a time.",
)

# Music
add(
    "Music",
    "Base rate",
    85,
    "Will a three-minute pop song put the chorus before the one-minute mark?",
    "Radio pop learned this shape a long time ago. In a typical chart run, about eight or nine in ten three-minute songs reach the chorus before a minute is gone. The verse is a hallway.",
)
add(
    "Music",
    "Base rate",
    90,
    "Will an encore include the biggest hit?",
    "Encores are a contract with the room. Across ordinary arena shows, about nine in ten save a known hit for the return. The deep cut was the middle of the set.",
)
add(
    "Music",
    "Vibes",
    31,
    "Will a guitar solo run longer than the verse?",
    "Solos feel endless and are often short. In a typical rock song, the solo outlasts the verse about three times in ten. The rest wave and get out of the way.",
)
add(
    "Music",
    "Base rate",
    35,
    "Will a festival set start within fifteen minutes of the printed time?",
    "Festival clocks are suggestions with wristbands. Across a typical weekend, only about a third of sets start inside a fifteen-minute window. The schedule was printed in hope.",
)
add(
    "Music",
    "Vibes",
    60,
    "Will a vinyl repress sell out its first colorway?",
    "Colorways are catnip for people who already own the album. In a typical small repress, about six in ten first colors sell through. Black vinyl stays for the rest of us.",
)
add(
    "Music",
    "Base rate",
    43,
    "Will a ballad change key in the last chorus?",
    "The key change is a tradition, not a requirement. Across ordinary ballads, a bit over four in ten lift the last chorus. The others stay in the key they started.",
)
add(
    "Music",
    "Base rate",
    24,
    "Will a live album include at least one false start?",
    "Most live albums are tidied before you hear them. In a typical decade of official live records, a false start survives about a quarter of the time. The rest were charming and then deleted.",
)
add(
    "Music",
    "Vibes",
    72,
    "Will a playlist named focus include a song with lyrics?",
    "Focus playlists promise instrumental calm and then sneak in a chorus. Across ordinary ones, about seven in ten include at least one song with words. Concentration is on its own.",
)
add(
    "Music",
    "Vibes",
    51,
    "Will the bridge be the best part and still get skipped?",
    "Bridges do the interesting work in the part people jump. In a typical listening session, about half of beloved bridges still get skipped on the second pass. The chorus has the remote.",
)
add(
    "Music",
    "Base rate",
    66,
    "Will a band tune on stage before the first song?",
    "Some rooms tune in the dark and some do it in front of you. Across ordinary club shows, about two thirds tune where you can see it. The lights come up on a peg, not a chord.",
)
add(
    "Music",
    "Base rate",
    78,
    "Will a holiday song return to daytime radio in December?",
    "December has a short memory and a long playlist. In a typical year, familiar holiday songs are back on daytime radio about eight times in ten once the month turns. The other stations hold out for a week.",
)
add(
    "Music",
    "Vibes",
    40,
    "Will a drummer count the band in out loud?",
    "Clicks and nods do a lot of quiet work. Across ordinary shows, an audible count-in happens about four times in ten. The rest start on a look.",
)
add(
    "Music",
    "Base rate",
    69,
    "Will a deluxe edition add fewer than five new songs?",
    "Deluxe often means a few extras, not a second album. In a typical release cycle, about seven in ten deluxe editions add fewer than five songs. The original track list did the heavy lifting.",
)
add(
    "Music",
    "Base rate",
    87,
    "Will someone in the crowd record a whole song in portrait?",
    "Vertical video won the room. At a typical show, nearly nine in ten songs are captured sideways by somebody. The stage is wide and the phone is not.",
)
add(
    "Music",
    "Vibes",
    74,
    "Will a music video cut on the snare?",
    "Editors love a hit they can see. Across ordinary performance videos, the cut lands with the snare about three quarters of the time. The other cuts are showing off.",
)
add(
    "Music",
    "Base rate",
    80,
    "Will an acoustic version be slower than the single?",
    "Unplugged usually means more air. In a typical pair of recordings, the acoustic take is slower about eight times in ten. Speed was living in the drum machine.",
)
add(
    "Music",
    "Base rate",
    58,
    "Will the drop in a dance track arrive within 45 seconds?",
    "Dance records tease, but not forever. Across ordinary club tracks, a bit over half drop inside 45 seconds. The long buildup is a specialty, not the default.",
)
add(
    "Music",
    "Vibes",
    64,
    "Will some band record a well-known jazz standard this year?",
    "Standards exist so someone can play them again. In a typical year, a familiar jazz tune gets a new recording more often than not. The song stays, and the players are new.",
)

# Sports banter
add(
    "Sports banter",
    "Base rate",
    56,
    "Will the home side score first?",
    "Home crowds do not score, but they come with a small edge. Across a typical decade of league games, the home side scores first a little more than half the time. The road team still gets the other half.",
)
add(
    "Sports banter",
    "Base rate",
    18,
    "Will a random league soccer match finish 1 to 0?",
    "Plenty of matches stay tight, and 1 to 0 is only one of those shapes. In a typical league season, fewer than two games in ten end exactly that way. Nil nil and 2 to 1 take their share.",
)
add(
    "Sports banter",
    "Base rate",
    9,
    "Will a random baseball game reach extra innings?",
    "Nine innings are usually enough. Across a typical season, fewer than one game in ten needs extras. The rest find a winner before the tenth.",
)
add(
    "Sports banter",
    "Vibes",
    61,
    "Will the underdog be the better story by Monday?",
    "Favorites win more boxes. Underdogs win more retellings. Across a typical weekend, the less favored side supplies the Monday story a bit over half the time, win or lose.",
)
add(
    "Sports banter",
    "Vibes",
    70,
    "Will a coach clap once before calling a timeout?",
    "Sideline body language is a dialect. In a typical game, a single clap shows up before a timeout about seven times in ten. The clipboard was already in motion.",
)
add(
    "Sports banter",
    "Base rate",
    92,
    "Will a tennis match include at least one ten-shot rally?",
    "Even big servers trade a few long points. Across ordinary matches, more than nine in ten include a rally of ten shots somewhere. The ace compilation leaves those out.",
)
add(
    "Sports banter",
    "Base rate",
    64,
    "Will a golf leaderboard change after the 15th hole on Sunday?",
    "Sunday afternoons are built for a moving card. In a typical final round, the lead or the chasing pack shifts after the 15th about two thirds of the time. The last three holes are a different sport.",
)
add(
    "Sports banter",
    "Base rate",
    12,
    "Will a marathon winner break the old course record?",
    "Course records are stubborn on purpose. Across a typical decade of the same race, the winner beats the old mark about once in eight runnings. Fast days are famous because they are rare.",
)
add(
    "Sports banter",
    "Vibes",
    48,
    "Will a penalty kick go to the keeper's left?",
    "Left and right are a guessing game with a small lean, not a code. Across ordinary spot kicks, the ball goes to the keeper's left a bit under half the time. The middle stays available and underused.",
)
add(
    "Sports banter",
    "Base rate",
    18,
    "Will a random hockey game include a fight?",
    "Highlight reels remember every fight and skip the quiet nights. In a typical recent season, a fight shows up in fewer than two games in ten. The whistle does most of the talking.",
)
add(
    "Sports banter",
    "Base rate",
    70,
    "Will the team ahead at halftime win a basketball game?",
    "A halftime lead is a head start, not a trophy. Across a typical league season, the team in front at the half wins about seven games in ten. Comebacks are why the second half exists.",
)
add(
    "Sports banter",
    "Base rate",
    8,
    "Will a relay team drop the baton at a championship meet?",
    "Baton passes are practiced until they look boring, which is the point. At a typical championship, a drop happens in well under one exchange in ten. The groan is loud because the miss is rare.",
)
add(
    "Sports banter",
    "Base rate",
    33,
    "Will a club player flag on time in a rapid chess game?",
    "Rapid clocks punish a long think. Across ordinary club rapid games, about a third end with a flag rather than a checkmate. The rest finish while both clocks still have a sliver.",
)
add(
    "Sports banter",
    "Vibes",
    27,
    "Will the crowd do the wave at least once?",
    "The wave needs boredom and a leader. In a typical regular-season game, it happens about a quarter of the time. Playoff crowds are often too busy yelling.",
)
add(
    "Sports banter",
    "Vibes",
    44,
    "Will one referee call get booed by both sides?",
    "Some whistles annoy only one bench. Across ordinary games, a call that both crowds dislike shows up a bit under half the time. Neutrality is not the same as popularity.",
)
add(
    "Sports banter",
    "Base rate",
    36,
    "Will the less favored side win outright on a random Saturday?",
    "Favorites are favorites for a reason. Across a typical decade of even-looking slates, the less favored side wins about three or four games in ten. That is enough to keep the group chat loud.",
)
add(
    "Sports banter",
    "Vibes",
    50,
    "Will overtime feel fairer than a coin flip and play about the same?",
    "Extra time has running and sweat, which makes it feel earned. The result is still close to a coin flip between two tired teams. Feeling fair and being a toss-up can both be true.",
)
add(
    "Sports banter",
    "Base rate",
    22,
    "Will a field goal try from midfield be good?",
    "Long kicks make the highlight because they are not the usual make. Across ordinary tries from midfield range, about one in five goes through. The rest become a punt in disguise.",
)

# Food
add(
    "Food",
    "Vibes",
    38,
    "Will the pizza arrive lukewarm?",
    "Most pies survive the trip. Across ordinary deliveries, a bit under four in ten show up cooler than you hoped. The box is an insulator with a time limit.",
)
add(
    "Food",
    "Base rate",
    71,
    "Will someone at the table say it needs more salt?",
    "Salt opinions are a side dish. In a typical shared meal, about seven in ten include one person who wants another pinch. The cook already salted it.",
)
add(
    "Food",
    "Base rate",
    68,
    "Will a new neighborhood restaurant still be open two years later?",
    "The scary failure stories are louder than the ordinary survivors. In a typical group of ten new neighborhood places, about seven are still serving two years on. The menu may have shrunk.",
)
add(
    "Food",
    "Vibes",
    22,
    "Will the avocado be perfect on the day you need it?",
    "Avocados have a window measured in hours. Across ordinary weeks, the one you bought is perfect on the planned day about twice in ten. Yesterday it was a rock, and tomorrow it is guacamole by force.",
)
add(
    "Food",
    "Vibes",
    41,
    "Will a sourdough starter survive a two-week vacation?",
    "A starter is a pet that eats flour. Across ordinary trips of two weeks, about four in ten starters come back lively without a rescue feeding. The rest need a pep talk and rye.",
)
add(
    "Food",
    "Base rate",
    80,
    "Will the office microwave smell like popcorn by midafternoon?",
    "Somebody always has a bag. In a typical office day, the microwave carries a popcorn note by midafternoon about eight times in ten. The fish day is a different genre.",
)
add(
    "Food",
    "Base rate",
    64,
    "Will a diner refill the coffee before you ask?",
    "Diners run on eye contact and a pot. Across ordinary counter seats, the refill arrives unasked about two thirds of the time. The cup was part of the conversation.",
)
add(
    "Food",
    "Vibes",
    58,
    "Will the middle french fry be the cold one?",
    "Bags cool from the center out less than you think, and still the middle one disappoints. In a typical carton, the center fry is the cool one a bit over half the time. The corner fry knew the heat lamp.",
)
add(
    "Food",
    "Base rate",
    76,
    "Will a barbecue cook say five more minutes and then take twenty?",
    "Low and slow has its own clock. Across ordinary backyard cooks, the five more minutes becomes twenty about three quarters of the time. The guests were warned by the smoke.",
)
add(
    "Food",
    "Vibes",
    47,
    "Will the grocery store have moved the oats since last month?",
    "Stores rearrange just enough to make you wander. In a typical month, the oats have moved about half the time. The milk stays put because everyone would revolt.",
)
add(
    "Food",
    "Vibes",
    29,
    "Will a homemade birthday cake lean a little?",
    "Home cakes are allowed a personality. Across ordinary birthday bakes, a visible lean shows up about three times in ten. Frosting is a construction material.",
)
add(
    "Food",
    "Base rate",
    53,
    "Will hot sauce get added after the first bite?",
    "The first bite is a test, not a verdict. At a typical table with a bottle nearby, hot sauce arrives after that bite about half the time. Some plates never needed it.",
)
add(
    "Food",
    "Vibes",
    67,
    "Will leftovers taste better the next day?",
    "A night in the fridge lets spices sit down together. Across ordinary stews, curries, and sauces, about two thirds taste better on day two. Lettuce is not invited to this rule.",
)
add(
    "Food",
    "Base rate",
    73,
    "Will a market tomato beat the one from the crisper?",
    "Season and a short trip do most of the work. In a typical summer comparison, the market tomato wins about three times in four. Winter makes the contest closer and sadder.",
)
add(
    "Food",
    "Vibes",
    44,
    "Will the ice cream line be longer than the menu is interesting?",
    "A good scoop shop earns a wait, and a long line can also mean one flavor. Across ordinary sunny afternoons, the line outshines the menu a bit under half the time. Vanilla is still doing its job.",
)
add(
    "Food",
    "Base rate",
    36,
    "Will the first side of a pancake stay on too long?",
    "The first pancake is a sacrifice and the first side is a guess. In a typical batch, about a third of opening pancakes go too far on side one. The second one has the information.",
)
add(
    "Food",
    "Base rate",
    42,
    "Will someone burn the garlic?",
    "Garlic goes from sweet to bitter in a few seconds. Across ordinary weeknight pans, it burns about four times in ten. The olive oil was patient and the cook was not.",
)
add(
    "Food",
    "Physics says no",
    4,
    "Will a scoop of ice cream stay firm for an hour on a sunny dashboard?",
    "A closed car in the sun is an oven with seats. An hour on that dashboard melts a scoop almost every time. Firm ice cream after that wait would need a different branch of physics.",
)

# Everyday life
add(
    "Everyday life",
    "Base rate",
    68,
    "Will the elevator stop at least once before the lobby?",
    "Lobbies are the last stop for a reason. In a typical daytime ride of more than three floors, the car stops early about two thirds of the time. Someone always joins at floor two.",
)
add(
    "Everyday life",
    "Base rate",
    45,
    "Will a pen from the junk drawer actually write?",
    "Junk drawers are pen museums. Across ordinary grabs, a bit under half of the pens still make a mark. The rest are dry souvenirs.",
)
add(
    "Everyday life",
    "Base rate",
    34,
    "Will the first sock you pull have its match in the same reach?",
    "Dryers are chaos with a lint screen. In a typical load, the first sock finds its pair on the first reach about a third of the time. The other sock is in the sleeve.",
)
add(
    "Everyday life",
    "Vibes",
    19,
    "Will the bus be early on the one morning you are late?",
    "Buses run early less often than stress suggests. Across ordinary commutes, the early departure lines up with your late morning about two times in ten. Most days the bus is merely itself.",
)
add(
    "Everyday life",
    "Base rate",
    81,
    "Will a group photo need a retake?",
    "Someone blinks or talks. In a typical group larger than four, the first frame fails about eight times in ten. The second frame is why phones have a burst mode.",
)
add(
    "Everyday life",
    "Vibes",
    52,
    "Will the umbrella be in the other bag?",
    "Rain plans and bag plans rarely share a brain. Across ordinary wet mornings, the umbrella is in the bag you did not bring about half the time. The other half you look prepared.",
)
add(
    "Everyday life",
    "Vibes",
    63,
    "Will the queue you switch into move slower?",
    "Switching feels clever and often is not. In a typical pair of lines, the one you join after switching moves slower about six times in ten. The original line takes it personally.",
)
add(
    "Everyday life",
    "Base rate",
    27,
    "Will the office printer jam on page one?",
    "Printers jam, just not every time you look at them. Across ordinary short jobs, page one jams about a quarter of the time. The rest print and still feel like a victory.",
)
add(
    "Everyday life",
    "Base rate",
    40,
    "Will a Monday meeting start within two minutes of the invite?",
    "Calendars are optimistic documents. In a typical Monday block, the meeting starts inside a two-minute window about four times in ten. The other starts are a hunt for the link.",
)
add(
    "Everyday life",
    "Vibes",
    48,
    "Will you forget why you walked into the room?",
    "Doorways are famous for this and still not a guarantee. Across ordinary trips between rooms, the reason survives about half the time. The other half you open a cabinet for clues.",
)
add(
    "Everyday life",
    "Base rate",
    37,
    "Will the charger already be in the room you are in?",
    "Chargers migrate to the last device that was dying. In a typical evening, the one you need is already in the room about a third of the time. The rest are on a desk you left.",
)
add(
    "Everyday life",
    "Vibes",
    22,
    "Will a tracking page say delivered while the box is still on the truck?",
    "Scans jump ahead of porch reality now and then. Across ordinary deliveries, the premature delivered scan happens about twice in ten. The other boxes match the page.",
)
add(
    "Everyday life",
    "Vibes",
    33,
    "Will the weather app and the window disagree?",
    "Apps average a region and windows see one street. In a typical week, they disagree in a way you can notice about a third of the days. The cloud did not read the hourly.",
)
add(
    "Everyday life",
    "Base rate",
    29,
    "Will a group chat actually settle on a time?",
    "Polls and maybes multiply. Across ordinary plans for more than four people, a firm time sticks about three times in ten. The rest become a soft evening.",
)
add(
    "Everyday life",
    "Base rate",
    56,
    "Will the scissors be in the knife drawer?",
    "Scissors have one correct home and several actual ones. In a typical search, they are in the knife drawer a bit over half the time. The other searches end at the tape.",
)
add(
    "Everyday life",
    "Vibes",
    46,
    "Will a quick errand take more than an hour?",
    "Quick is the story you tell when you leave. Across ordinary errands with one stop, a bit under half expand past an hour. The extra stop was not on the list.",
)
add(
    "Everyday life",
    "Base rate",
    78,
    "Will the good parking spot be taken?",
    "The spot by the door is a shared myth. In a typical busy hour, it is already taken about eight times in ten. The longer walk was always the plan.",
)
add(
    "Everyday life",
    "Base rate",
    43,
    "Will the houseplant get watered on the day you intended?",
    "Plants live on a calendar that people mean to keep. Across ordinary weeks, the intended watering day is honored a bit over four times in ten. The plant has learned patience.",
)

# Space and science
add(
    "Space and science",
    "Physics says no",
    2,
    "Will a house cat reach orbit by jumping off a fridge?",
    "Orbit needs many thousands of kilometers an hour, not a good crouch. A fridge jump ends in a kitchen. No cat, however confident, is clearing the atmosphere from a countertop.",
)
add(
    "Space and science",
    "Base rate",
    62,
    "Will toast that slips off a table land butter side down?",
    "A typical table height gives a falling slice about half a turn before the floor. That is why butter side down shows up more often than a fair coin. A very tall table would tell a different joke.",
)
add(
    "Space and science",
    "Base rate",
    75,
    "Will an hour under a clear dark sky include at least one meteor?",
    "Sporadic meteors show up even outside a named shower. On a typical clear dark night, a patient hour includes at least one. City glow is what usually cancels the show.",
)
add(
    "Space and science",
    "Vibes",
    90,
    "Will a full moon look larger when it sits near the horizon?",
    "The moon is not actually bigger down low. A typical evening still makes it look that way, which is the moon illusion doing its old trick. The camera, unimpressed, shows the same disc.",
)
add(
    "Space and science",
    "Base rate",
    84,
    "Will a small backyard telescope show Saturn's rings on a steady night?",
    "The rings are wide enough for modest glass. On a typical steady night, a small telescope shows them about eight times in ten tries. Blurry air is the usual villain, not the planet.",
)
add(
    "Space and science",
    "Physics says no",
    3,
    "Will a pen dropped on a space station fall to the floor like it does in a kitchen?",
    "The station and the pen are both falling around Earth together. A dropped pen stays nearby instead of dropping to a floor. Kitchen gravity is the thing you left at home.",
)
add(
    "Space and science",
    "Base rate",
    97,
    "Will a person walking beat a garden snail over ten meters?",
    "A garden snail takes minutes to cover a meter. A person walking finishes ten meters before the snail finishes one. The snail is consistent, and the person is simply faster.",
)
add(
    "Space and science",
    "Base rate",
    92,
    "Will water boil below 100 degrees Celsius on a high mountain?",
    "Air is thinner up high, so the boiling point drops. On a typical high mountain, water boils below 100 degrees. The sea-level number is a hometown fact.",
)
add(
    "Space and science",
    "Base rate",
    95,
    "Will two magnets snap together if opposite poles are a hair apart?",
    "Opposite poles pull, and a hair of air is not a shield. In a typical pair of fridge magnets, they snap as soon as you relax your grip. Same poles would shove instead.",
)
add(
    "Space and science",
    "Base rate",
    15,
    "Will a paper airplane thrown indoors stay up for more than eight seconds?",
    "Indoor air is calm and the ceiling is close. Across ordinary paper planes, a flight past eight seconds happens about one time in seven. Most of them decorate the sofa sooner.",
)
add(
    "Space and science",
    "Base rate",
    88,
    "If a bright station pass goes overhead under a clear sky, will you see it without a telescope?",
    "A good overhead pass is a steady moving star. Under typical clear skies, you can see it with just your eyes about nine times in ten. Clouds are the only required equipment to ruin it.",
)
add(
    "Space and science",
    "Base rate",
    86,
    "Will a banana ripen faster in a closed paper bag?",
    "Bananas give off ethylene, and a bag keeps that gas nearby. In a typical kitchen test, the bagged one ripens faster about eight or nine times in ten. The counter banana takes the scenic route.",
)
add(
    "Space and science",
    "Base rate",
    93,
    "Will a helium balloon rise in a room full of ordinary air?",
    "Helium is lighter than the air around it. In a typical room, the balloon goes up. A leaking balloon eventually joins the party on the floor, which is a different question.",
)
add(
    "Space and science",
    "Base rate",
    96,
    "In a vacuum, will a feather and a hammer fall together?",
    "Air is what makes the feather late. Take the air away and they fall together, which is what the classic vacuum demo shows. The hammer does not get a special exemption.",
)
add(
    "Space and science",
    "Base rate",
    40,
    "Will you hear thunder from a small storm more than 20 kilometers away?",
    "Sound from a small storm often fades before it has traveled 20 kilometers. Big storms carry farther. Across ordinary rumbles, hearing one from that far is a bit under half.",
)
add(
    "Space and science",
    "Base rate",
    90,
    "Will the North Star barely move in a long exposure photo?",
    "The sky turns around a point very near Polaris. In a typical long exposure from the north, that star stays almost still while the others draw arcs. South of the equator you are photographing a different trick.",
)
add(
    "Space and science",
    "Base rate",
    8,
    "Will a total solar eclipse cross your exact town in the next ten years?",
    "A total eclipse path is a narrow strip. Most towns wait decades between them, so a ten-year window is usually a miss. Partial shade is a much more common visitor.",
)
add(
    "Space and science",
    "Physics says no",
    2,
    "Will a shout on the Moon be heard by someone standing a field away, with no radio?",
    "Sound needs a medium, and the Moon has no air to carry it. A shout stays in the helmet. Across the field, silence is the whole forecast.",
)

# Pets
add(
    "Pets",
    "Base rate",
    72,
    "Will a cat knock something off a table this week?",
    "Tables are shelves that cats audit. In a typical week with a cat and a cluttered surface, something gets nudged off about seven times in ten. The object was always a candidate.",
)
add(
    "Pets",
    "Base rate",
    88,
    "Will a dog greet you like the separation was enormous?",
    "Dogs do not do casual reentries. Across ordinary homecomings, even after a short trip, the full ceremony happens nearly nine times in ten. The tail was already in motion.",
)
add(
    "Pets",
    "Base rate",
    91,
    "Will a cat sit in a box that is slightly too small?",
    "If the box exists, the fit is a suggestion. In a typical house with a shipping box, the cat chooses the too-small one about nine times in ten. Comfort was never the brief.",
)
add(
    "Pets",
    "Base rate",
    77,
    "Will a dog pick the sunny patch on the floor?",
    "Sun squares move, and dogs follow them. Across ordinary afternoons, the dog is in the bright patch about three quarters of the time. The rug was only a theory.",
)
add(
    "Pets",
    "Base rate",
    80,
    "Will a pet ignore the expensive toy and play with the box?",
    "Packaging has crunch and novelty. In a typical unboxing, the pet chooses the box over the toy about eight times in ten. The toy gets a fair trial later, maybe.",
)
add(
    "Pets",
    "Vibes",
    46,
    "Will a cat slow-blink back if you slow-blink first?",
    "Some cats return a slow blink and plenty do not. Across a typical week of trying, it lands near a coin flip. The cat may have understood, and participation stays optional.",
)
add(
    "Pets",
    "Base rate",
    69,
    "Will a dog bring the ball back and then refuse to release it?",
    "Fetch has a negotiation phase. In a typical session, the return-and-refuse happens about seven times in ten. The game was retrieval, and the terms were unclear.",
)
add(
    "Pets",
    "Vibes",
    41,
    "Will a cat sit on the keyboard during a call?",
    "Calls are warm laps with extra attention. Across ordinary video calls in a cat house, the keyboard gets sat on about four times in ten. The mute button was the only plan.",
)
add(
    "Pets",
    "Base rate",
    35,
    "Will a rabbit thump when a stranger walks in?",
    "Thumps are alarms, not greetings. In a typical visit, a house rabbit thumps at a new person about a third of the time. The other welcomes are a cautious sniff.",
)
add(
    "Pets",
    "Vibes",
    54,
    "Will a parrot pick one word and use it too often?",
    "Parrots are editors with a favorite line. Across ordinary talking birds, one word gets overused about half the time. The rest of the vocabulary is on break.",
)
add(
    "Pets",
    "Base rate",
    63,
    "Will a dog tilt its head at a high or odd question?",
    "The tilt shows up when a sound is almost familiar. In a typical bout of silly questions, a dog tilts about six times in ten. The other questions get a blink.",
)
add(
    "Pets",
    "Base rate",
    82,
    "Will a cat choose an empty bag over the cat bed?",
    "Beds are purchased by humans and bags are discovered. Across ordinary evenings, the bag wins about eight times in ten. The bed remains a decorative suggestion.",
)
add(
    "Pets",
    "Base rate",
    58,
    "Will a hamster run the wheel during the hour you wanted quiet?",
    "Hamsters keep their own night. In a typical evening, the wheel starts during the quiet hour a bit over half the time. Earplugs are the roommate agreement.",
)
add(
    "Pets",
    "Vibes",
    33,
    "Will a dog steal a sock and ignore the shoe?",
    "Socks are lighter and more available. Across ordinary thefts, the sock is the target and the shoe stays put about a third of the time. The other dogs have broader taste.",
)
add(
    "Pets",
    "Base rate",
    86,
    "Will a fish notice you when it is feeding time?",
    "Fish learn a shadow and a schedule. At a typical feeding, they gather when you approach about eight or nine times in ten. The rest are busy being fish.",
)
add(
    "Pets",
    "Base rate",
    60,
    "Will a cat chirp at a bird through the window?",
    "Windows are nature documentaries with no remote. Across ordinary bird visits, a chirp or chatter happens about six times in ten. The other watches are silent and intense.",
)
add(
    "Pets",
    "Base rate",
    74,
    "Will a dog wait by the door before the usual walk?",
    "Dogs can read a routine better than a clock. In a typical household, the pre-walk post by the door happens about three days in four. The leash is a formality.",
)
add(
    "Pets",
    "Vibes",
    79,
    "Will a kitten nap in the clean laundry?",
    "Warm fabric outranks every purchased bed. Across ordinary laundry days with a kitten, the clean pile becomes a nest about eight times in ten. Folding was a temporary state.",
)


def sentences(text):
    parts = []
    buf = []
    for ch in text:
        buf.append(ch)
        if ch in ".!?":
            piece = "".join(buf).strip()
            if piece:
                parts.append(piece)
            buf = []
    tail = "".join(buf).strip()
    if tail:
        parts.append(tail)
    return parts


def main():
    categories = [
        "Pop culture",
        "Memes and internet",
        "Movies and TV",
        "Music",
        "Sports banter",
        "Food",
        "Everyday life",
        "Space and science",
        "Pets",
    ]
    bad = []
    seen = set()
    counts = {name: 0 for name in categories}
    for row in ROWS:
        key = row["question"].strip().lower()
        if key in seen:
            bad.append("dup " + row["question"])
        seen.add(key)
        if row["category"] not in counts:
            bad.append("bad category " + row["category"])
        else:
            counts[row["category"]] += 1
        if not isinstance(row["likelihood"], int) or not 1 <= row["likelihood"] <= 99:
            bad.append("likelihood " + row["question"])
        if row["tag"] not in {"Base rate", "Vibes", "Physics says no"}:
            bad.append("tag " + row["question"])
        bits = sentences(row["reasoning"])
        if len(bits) not in (2, 3):
            bad.append(f"sentences {len(bits)} {row['question']}")
        for banned in ("\u2014", "\u2013", "\u2026"):
            if banned in row["question"] or banned in row["reasoning"]:
                bad.append("dash " + row["question"])
        if any(ord(ch) > 127 and ch not in "’" for ch in row["question"] + row["reasoning"]):
            # allow plain ascii only
            extra = [ch for ch in row["question"] + row["reasoning"] if ord(ch) > 127]
            bad.append("nonascii " + row["question"] + " " + repr(extra))
    print("count", len(ROWS))
    print(counts)
    if bad:
        print("PROBLEMS", len(bad))
        for item in bad:
            print(item)
        raise SystemExit(1)
    path = Path(__file__).with_name("questions.json")
    path.write_text(json.dumps(ROWS, indent=2) + "\n", encoding="utf-8")
    print("wrote", path)


if __name__ == "__main__":
    main()
