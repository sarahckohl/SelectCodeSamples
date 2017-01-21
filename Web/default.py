# -*- coding: utf-8 -*-
# this file is released under public domain and you can use without limitations


import logging
import json, urllib
import re

APIKEY = "97ef62ee-275d-48ed-baa4-d8f5538eb5de"

def index():
    searchBar = SQLFORM.factory(
                                Field('body', 'string', default="Type a summoner name.", label='Seach'))    
    
    if searchBar.process(formname='searchBar').accepted:
        redirect(URL('default', 'summoner',args=[searchBar.vars.body]))
        
    return dict(searchBar=searchBar)


#really,version numbers should probably be made global
def getSummonerID(summonerName):
    
    url = "https://na.api.pvp.net/api/lol/na/v1.4/summoner/by-name/"+summonerName+"?api_key="+APIKEY
    response = urllib.urlopen(url)
    
    if response.code != 200:
        return None
    
    data = json.loads(response.read())
    sid = data.values()[0].get('id', None)
    return str(sid)



def canReview(mySummonerID, anotherSummoner):
     
     
    matchHistoryURL = "https://na.api.pvp.net/api/lol/na/v2.2/matchhistory/"+mySummonerID+"?endIndex=15&api_key="+APIKEY
    response = urllib.urlopen(matchHistoryURL)
    matchHistoryJSON = json.loads(response.read())
    matchesList = matchHistoryJSON.values()[0]
    
    if response.code != 200:
        logger.info("Bad stuff")
        return True
    
    canBeReviewed = False
      
    "This is not really ideal, but I'm not sure if it can be made better barring just being approved for more requests/sec."
    MAXIMUMREQUESTSPERSECOND = 1
    for i in range(0,MAXIMUMREQUESTSPERSECOND):
           
        if canBeReviewed == True:
            break 
        aMatchID = matchesList[i].get('matchId', None)
       
        "With the MatchID, you can now get match(es)"
       
        matchDetailsURL = "https://na.api.pvp.net/api/lol/na/v2.2/match/"+str(aMatchID)+"?includeTimeline=false&api_key="+APIKEY
        response = urllib.urlopen(matchDetailsURL)
        matchDetailsJSON = json.loads(response.read())
       
       
        for item in matchDetailsJSON.get("participantIdentities", None):
            aSummonerName = item.values()[0].get('summonerName', None)
            #may need to just pass another parameter for summoner instead of reqeust.args(0)
            if aSummonerName == anotherSummoner and anotherSummoner != str(request.args(0)):
                canBeReviewed = True
                break
            else:
                continue
      
    return canBeReviewed

@auth.requires_login
def validation():
    mySummonerID = auth.user['summoner']
    runesURL = "https://na.api.pvp.net/api/lol/na/v1.4/summoner/"+mySummonerID+"/runes?api_key="+APIKEY
    response = urllib.urlopen(summonerSummaryURL)
    summonerSummary = json.loads(response.read())

    testObject = ""# summonerSummary.values[0]""

    return dict(testObject=testObject)    


def getWL(mySummonerID):
        #note the season.  as a stretch goal, we could do tabs
    summonerSummaryURL = "https://na.api.pvp.net/api/lol/na/v1.3/stats/by-summoner/"+mySummonerID+"/summary?season=SEASON2015&api_key="+APIKEY
    response = urllib.urlopen(summonerSummaryURL)
    summonerSummary = json.loads(response.read())
     
    #the different queues are all in an inconsistently (by summoner) ordered list, so the best thing I know to do is just search for a match
    for i in range(0,len(summonerSummary.get('playerStatSummaries', None))):
        if summonerSummary.get('playerStatSummaries', None)[i].get('playerStatSummaryType', None) == "RankedSolo5x5":
            SoloRanksStats = summonerSummary.get('playerStatSummaries', None)[i]
            break
         
    RankedWins = SoloRanksStats.get('wins', None)
    RankedLosses = SoloRanksStats.get('losses', None)     
    
    return (RankedWins, RankedLosses)

def countStreak(mySummonerID):
         
    #gets 15 games, which may or may not be ok to do if they have played less than that
    #matchHistoryURL = "https://na.api.pvp.net/api/lol/na/v2.2/matchhistory/"+mySummonerID+"?rankedQueues=RANKED_SOLO_5x5&endIndex=15&api_key=97ef62ee-275d-48ed-baa4-d8f5538eb5de"
    #eventually maybe make this solo, but it's a little easier to test if it's all ranked.  Nevermind, it seems to default to solo and not actually fetch all queues.
    matchHistoryURL = "https://na.api.pvp.net/api/lol/na/v2.2/matchhistory/"+mySummonerID+"?endIndex=15&api_key="+APIKEY
    response = urllib.urlopen(matchHistoryURL)
    matchHistoryJSON = json.loads(response.read())
    matchesList = matchHistoryJSON.values()[0]
     
    aMatch = matchesList[0]
    #True if a winstreak, false if a losestreak
    PositiveStreak = aMatch.get('participants', None)[0].get('stats', None).get('winner', None)
     
    for i in range(0,len(matchesList)):
        aMatch = matchesList[i]
        nextStreak = matchesList[i].get('participants', None)[0].get('stats', None).get('winner', None)
        if nextStreak == PositiveStreak:
            continue
        else:
            streakLength = i+1
            break
    streakType = "Win" if PositiveStreak else "Lose"
    return (streakType, streakLength)
    

summoner = str(request.args(0)) or None

@auth.requires_login() 
def summoner():    
    #eventually will want to use this as a cosnt instead of the hard-coded ones, but it's fine for designing since they give the request URLS on the website
    
    title = request.args(0) or 'main_page' #if request.args(0) is None, show the main wiki page
    summoner = str(request.args(0)) or None
    form = None
    content = None
    display_title = title.title()
    
    mySummonerID = getSummonerID(summoner)
    
    logger.info("summoner:")
    logger.info(summoner)
    logger.info("ID:")
    logger.info(mySummonerID)
    
    
    if mySummonerID == None:
        redirect(URL('default', 'index')) 
    
    
    
    if summoner:
        display_title = summoner

 
    "We will probably divide this logic out into at least 1 more function, if not 2 or 3"
    "Consider returning W:L as a tuple"
     
    WRTuple = getWL(mySummonerID)
    RankedWins = WRTuple[0]
    RankedLosses = WRTuple[1]
     
    "End of W:R Code"
     
     
    "This block of code will grab a lot of the information about current league standing, including Hotstreak status, miniseries status, etc."
    #Note that the league request can return NULL of the person is unranked!!
    leagueURL = "https://na.api.pvp.net/api/lol/na/v2.5/league/by-summoner/"+mySummonerID+"/entry?api_key="+APIKEY
    response = urllib.urlopen(leagueURL)
    
    if response.code != 200:
        summonerTier = None
        summonerDivision = None
        summonerisHotStreak = None
        summonerMiniSeries = None
    else:
        leagueJSON = json.loads(response.read())    
        summonerTier = leagueJSON.values()[0][0].get('tier', None)
        summonerDivision = leagueJSON.values()[0][0].get('entries', None)[0].get('division', None)
        summonerisHotStreak = leagueJSON.values()[0][0].get('entries', None)[0].get('isHotStreak', None)
    #maybe if this is defined it would be a good idea to show what their current miniseries standing is (people tilt in their final)
    #relevent fields...
    #miniSeries.progress (string representing progress i.e. WLN or LWN would be the ones to look out for)
    #miniSeries.losses
    #miniSeries.wins
    #miniSeries.target (wins required to promote) 
        summonerMiniSeries = leagueJSON.values()[0][0].get('entries', None)[0].get('miniSeries', None)
     
    "End of league related code"

     
    "Streak-related code: find their current win or lose streak"
    streakTuple = countStreak(mySummonerID)
    streakType = streakTuple[0]
    streakLength = streakTuple[1]
    "End of Streak related code"
     
     
    "Posting Authorization"
    "This code is to control abuse cases like reviewing people you haven't played with recently.  It currently does only that, but I will eventually"
    "also make it so you can't double post for the same match and things like that."
    someJerk = auth.user['summoner'] or None
    
    userCanReview = canReview(mySummonerID, someJerk)
    canReviewString = "You ("+someJerk+") can review this summoner!" if userCanReview  else "You ("+someJerk+") can NOT review this summoner!" 
    #canReviewString = someJerk+" can review this summoner!" if True else someJerk+" can NOT review this summoner!" 
     
    "End of posting authorization code."
     
     
    "Page search bar"
    "Will probably want to authenticate for valid summoners here.  May also want to add auto-completion, but"
    "That might be better redone with server-side scriptng (jquery)"
    searchBar = SQLFORM.factory(
                                Field('body', 'string', default="Type a summoner name.", label='Seach'))    
    
    if searchBar.process(formname='searchBar').accepted:
        redirect(URL('default', 'summoner',args=[searchBar.vars.body]))
    "End of search-bar logic"
    
    
    
    page = db(db.pagetable.title == title).select().first()
    if page is None:
        db.pagetable.insert(title=title)
        page = db(db.pagetable.title==title).select().first()
        page_id = page.id
    else:
        page_id = page.id 

    # Are we editing?
    editing = request.vars.edit == 'true'
    if editing and userCanReview:
        # We are editing.  Gets the body s of the page.
        # Creates a form to edit the content s, with s as default.
        
        print(page_id)

        form = SQLFORM.factory(
                                    Field('Recommended', type='boolean'),
                                    Field('body', 'text',
                                  label='Review',widget=markitup_widget, default=""
                                ))

        if form.process(formname='reviewForm').accepted:
            db.revision2.insert(rev_id=page_id, body=form.vars.body, reviewer=auth.user['summoner'], recommended=form.vars.Recommended)
            redirect(URL('default', 'summoner', args=[title]))      
        
        content = form

        # We are just displaying the page
        #q=db(db.revision.page_id == page_id) 
    return dict(display_title=display_title,
                content=content, 
                editing=editing, 
                title=summoner, 
                sid=mySummonerID,
                RankedWins=RankedWins, 
                RankedLosses=RankedLosses,
                summonerTier = summonerTier,
                #leagueJSON=leagueJSON,
                summonerDivision=summonerDivision,
                summonerisHotStreak=summonerisHotStreak,
                summonerMiniSeries=summonerMiniSeries,
                testObject = db(db.revision2.rev_id==page_id).select(),
                streakType = streakType,
                streakLength = streakLength,
                canReviewString = canReviewString,
                searchBar=searchBar,
                userCanReview=userCanReview
                )


def markitup_widget(field, value):
    return TEXTAREA(value,
                    _name=field.name,
                    _id="%s_%s" % (field._tablename, field.name),
                    _class='markitup',
                    requires=field.requires)

def user():
    """
    exposes:
    http://..../[app]/default/user/login
    http://..../[app]/default/user/logout
    http://..../[app]/default/user/register
    http://..../[app]/default/user/profile
    http://..../[app]/default/user/retrieve_password
    http://..../[app]/default/user/change_password
    http://..../[app]/default/user/manage_users (requires membership in
    use @auth.requires_login()
        @auth.requires_membership('group name')
        @auth.requires_permission('read','table name',record_id)
    to decorate functions that need access control
    """
    return dict(form=auth())


@cache.action()
def download():
    """
    allows downloading of uploaded files
    http://..../[app]/default/download/[filename]
    """
    return response.download(request, db)


def call():
    """
    exposes services. for example:
    http://..../[app]/default/call/jsonrpc
    decorate with @services.jsonrpc the functions to expose
    supports xml, json, xmlrpc, jsonrpc, amfrpc, rss, csv
    """
    return service()


@auth.requires_login() 
def api():
    """
    this is example of API with access control
    WEB2PY provides Hypermedia API (Collection+JSON) Experimental
    """
    from gluon.contrib.hypermedia import Collection
    rules = {
        '<tablename>': {'GET':{},'POST':{},'PUT':{},'DELETE':{}},
        }
    return Collection(db).process(request,response,rules)