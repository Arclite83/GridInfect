#include "ClassicMenuScene.h"
#include "Enums.h"
#include "FreePlayMenuScene.h"
#include "Game.h"
#include "GPGSManager.h"
#include "Level.h"
#include "LevelMenuScene.h"
#include "SaveData.h"
#include "SoundManager.h"

USING_NS_CC;

CCScene* LevelMenu::scene()
{
    // 'scene' is an autorelease object
    CCScene *scene = CCScene::create();
    
    // 'layer' is an autorelease object
    LevelMenu *layer = LevelMenu::create();

    // add layer as a child to scene
    scene->addChild(layer);

    // return the scene
    return scene;
}

//board sprites: 0-XX (save room! 300?)
//CCSprite* _boardSprites[Level::Height * Level::Width];

//piece sprites: 300-307
//CCSprite* _pieceSprites[8];

//level text: 400
//CCLabelTTF* _levelText;

//CCSprite* _popupBg; //500
//CCMenuItemImage* _beginButton; //501
//CCLabelTTF* _popupText; //502
//CCMenuItemImage* _menuButton; //503
//CCMenuItemImage* _replayButton; //504
//CCMenuItemImage* _nextButton; //505
//CCMenuItemImage* _menuFramedButton; //506
//CCMenuItemImage* _replayFramedButton; //507

//Event Placeholder: 999
//Mute: 1000

int _touchedPiece;

int _timeStarted;
int _timeStopped;

// on "init" you need to initialize your instance
bool LevelMenu::init()
{
    if ( !CCLayer::init() )
    {
        return false;
    }
    
    CCSize visibleSize = CCDirector::sharedDirector()->getVisibleSize();
    CCPoint origin = CCDirector::sharedDirector()->getVisibleOrigin();
    
    CCSprite* eventPlaceholder = CCSprite::create("event_placeholder.png");
    eventPlaceholder->setPosition(ccp(-10, -10));
    eventPlaceholder->setTag(999);
    this->addChild(eventPlaceholder, 0);

    CCSprite* backgroundSprite = CCSprite::create("background.png");
    backgroundSprite->setPosition(ccp(visibleSize.width/2 + origin.x, visibleSize.height/2 + origin.y));
    backgroundSprite->setScaleX(visibleSize.width/backgroundSprite->getContentSize().width);
    backgroundSprite->setScaleY(visibleSize.height/backgroundSprite->getContentSize().height);
    this->addChild(backgroundSprite, 0);
    
    if (Game::getInstance()->getMode() == Classic)
    {
        int levelId = Game::getInstance()->getLevelId();
        switch(levelId)
        {
			case 0:
            {
                CCSprite* msgSprite = CCSprite::create("message_1.png");
                msgSprite->setScale(visibleSize.height/msgSprite->getContentSize().height);
                float msgX = origin.x + visibleSize.width * 0.50f;
                float msgY = origin.y + visibleSize.height * 0.50f;
                msgSprite->setPosition(ccp(msgX, msgY));
                this->addChild(msgSprite, 0);
				break;
            }
            case 2:
            {
                CCSprite* msgSprite = CCSprite::create("message_3.png");
                msgSprite->setScale(visibleSize.height/msgSprite->getContentSize().height);
                float msgX = origin.x + visibleSize.width * 0.50f;
                float msgY = origin.y + visibleSize.height * 0.50f;
                msgSprite->setPosition(ccp(msgX, msgY));
                this->addChild(msgSprite, 0);
				break;
            }
			case 4:
            {
                CCSprite* msgSprite = CCSprite::create("message_5.png");
                msgSprite->setScale(visibleSize.height/msgSprite->getContentSize().height);
                float msgX = origin.x + visibleSize.width * 0.50f;
                float msgY = origin.y + visibleSize.height * 0.50f;
                msgSprite->setPosition(ccp(msgX, msgY));
                this->addChild(msgSprite, 0);
				break;
            }
			case 9:
            {
                CCSprite* msgSprite = CCSprite::create("message_10.png");
                msgSprite->setScale(visibleSize.height/msgSprite->getContentSize().height);
                float msgX = origin.x + visibleSize.width * 0.50f;
                float msgY = origin.y + visibleSize.height * 0.50f;
                msgSprite->setPosition(ccp(msgX, msgY));
                this->addChild(msgSprite, 0);
				break;
            }
            case 25:
            {
                CCSprite* msgSprite = CCSprite::create("message_26.png");
                msgSprite->setScale(visibleSize.height/msgSprite->getContentSize().height);
                float msgX = origin.x + visibleSize.width * 0.50f;
                float msgY = origin.y + visibleSize.height * 0.50f;
                msgSprite->setPosition(ccp(msgX, msgY));
                this->addChild(msgSprite, 0);
				break;
            }
			case 28:
            {
                CCSprite* msgSprite = CCSprite::create("message_29.png");
                msgSprite->setScale(visibleSize.height/msgSprite->getContentSize().height);
                float msgX = origin.x + visibleSize.width * 0.50f;
                float msgY = origin.y + visibleSize.height * 0.50f;
                msgSprite->setPosition(ccp(msgX, msgY));
                this->addChild(msgSprite, 0);
				break;
            }
        }
    }

    float tileOffsetX = origin.x + visibleSize.width / 2;
    float tileOffsetY = origin.y + visibleSize.height * 0.80f;
    float tileHeight = visibleSize.height * 0.11f;
    
    //create board based on level
    for (float i = 0; i < Level::Height; i++) //y
    {
        for (float j = 0; j < Level::Width; j++) //x
        {
            CCSprite* tileSprite = CCSprite::create("tile_gray.png");
            tileSprite->setScale(tileHeight/tileSprite->getContentSize().height);
            float tileWidth = tileSprite->getContentSize().width * tileSprite->getScale() * 1.05f;
            
            int x = tileOffsetX
                    + (tileWidth * (j - 5));
            
            int y = tileOffsetY
                    - i * (tileHeight * 1.05f);
            
            tileSprite->setPosition(ccp(x,y));
            tileSprite->setOpacity(0);
            this->addChild(tileSprite, 0);
            
            int loc = i * Level::Width + j;
            tileSprite->setTag(loc);
        }
    }
    
    //create pieces based on level
    for (int i = 0; i < 8; i++)
    {
        CCSprite* pieceSprite = CCSprite::create("piece_L.png");
        pieceSprite->setScale(visibleSize.height/pieceSprite->getContentSize().height * 0.11f);
        float pieceWidth = pieceSprite->getContentSize().width * pieceSprite->getScale() * 1.1f;
        float bugX = origin.x
                + visibleSize.width / 2
                + (pieceWidth * (float)((float)i - 3.0f) * 1.1f);
        pieceSprite->setPosition(ccp(bugX,
                                     origin.y
                                     + visibleSize.height * 0.075f));
        pieceSprite->setOpacity(0);
        pieceSprite->setTag(300 + i);
        this->addChild(pieceSprite, 0);
    }

    CCLabelTTF* levelText = CCLabelTTF::create("",
                                    "fonts/Overhaul.ttf",
                                    visibleSize.height * 0.06f);
    
    if (Game::getInstance()->getMode() == Classic)
    {
        std::stringstream s;
        s << "Level ";
        s << (Game::getInstance()->getLevelId() + 1);
        levelText->setString(s.str().c_str());
        levelText->setPosition(ccp(origin.x
                                   + visibleSize.width * 0.5f,
                                   origin.y
                                   + visibleSize.height * 0.99f
                                   - levelText->getContentSize().height));
    }
    else
    {
        levelText->setString("FREE PLAY");
        levelText->setPosition(ccp(origin.x
                                   + visibleSize.width * 0.45f,
                                   origin.y
                                   + visibleSize.height * 0.99f
                                   - levelText->getContentSize().height));
    }
    levelText->setTag(400);
    this->addChild(levelText, 0);
    
    CCMenuItemImage* popupBg = CCMenuItemImage::create("popup_bg.png",
                                                       "popup_bg_pressed.png",
                                                       this,
                                                       menu_selector(LevelMenu::beginButtonCallback));
    popupBg->setScale(visibleSize.height/popupBg->getContentSize().height * 0.68f);
    popupBg->setTag(500);
    
    //create pop-up menu and buttons
    if (Game::getInstance()->getMode() == Classic)
    {
        popupBg->setEnabled(false);
        popupBg->setPosition(ccp(origin.x
                                    + visibleSize.width * 0.5f,
                                    origin.y
                                    + visibleSize.height * 1.5f
                                    + popupBg->getContentSize().height/2));
    }
    else
    {
        popupBg->setEnabled(true);
        popupBg->setPosition(ccp(origin.x
                                  + visibleSize.width * 0.5f,
                                  origin.y
                                  + visibleSize.height * 0.5f));
        
        CCLabelTTF* beginText = CCLabelTTF::create("BEGIN",
                                                   "fonts/Overhaul.ttf",
                                                   visibleSize.height * 0.25f);
        beginText->setPosition(ccp(origin.x
                                   + visibleSize.width * 0.5f,
                                   origin.y
                                   + visibleSize.height * 0.5f
                                   - beginText->getContentSize().height * 0.1f));
        beginText->setTag(501);
        this->addChild(beginText, 2);
    }
    CCMenu* pMenuBegin = CCMenu::create(popupBg, NULL);
    pMenuBegin->setPosition(CCPointZero);
    pMenuBegin->setTag(500);
    this->addChild(pMenuBegin, 1);
    
    CCLabelTTF* popupText = CCLabelTTF::create("COMPLETE",
                                    "fonts/Overhaul.ttf",
                                    visibleSize.height * 0.10f);
    popupText->setPosition(ccp(origin.x
                                + visibleSize.width * 0.5f,
                                origin.y
                                + visibleSize.height * 1.55f
                                + popupText->getContentSize().height / 2));
    popupText->setTag(502);
    this->addChild(popupText, 2);
    
    CCMenuItemImage* menuButton = CCMenuItemImage::create(
                                                          "btn_menu.png",
                                                          "btn_menu.png",
                                                          this,
                                                          menu_selector(LevelMenu::menuCallback));
    menuButton->setScale(visibleSize.height/menuButton->getContentSize().height * 0.11f);
    menuButton->setPosition(ccp(origin.x
                                + visibleSize.width * 0.5f,
                                origin.y
                                + visibleSize.height * 1.25f
                                + menuButton->getContentSize().height/2 * menuButton->getScaleY()));
    menuButton->setTag(503);
    CCMenu* pMenuMenu = CCMenu::create(menuButton, NULL);
    pMenuMenu->setPosition(CCPointZero);
    pMenuMenu->setTag(503);
    this->addChild(pMenuMenu, 2);
    
    CCMenuItemImage*menuFramedButton = CCMenuItemImage::create(
                                                "btn_menu_framed.png",
                                                "btn_menu_framed_pressed.png",
                                                this,
                                                menu_selector(LevelMenu::menuCallback));
    menuFramedButton->setScale(visibleSize.height/menuFramedButton->getContentSize().height * 0.11f);
    menuFramedButton->setPosition(ccp(origin.x
                                       + visibleSize.width * 0.98f
                                       - menuFramedButton->getContentSize().width/2 * menuFramedButton->getScaleX(),
                                       origin.y
                                       + visibleSize.height * 0.02f
                                       + menuFramedButton->getContentSize().height/2 * menuFramedButton->getScaleY()));
    menuFramedButton->setTag(506);
    CCMenu* pMenuMenuFramed = CCMenu::create(menuFramedButton, NULL);
    pMenuMenuFramed->setPosition(CCPointZero);
    pMenuMenuFramed->setTag(506);
    this->addChild(pMenuMenuFramed, 1);
    
    CCMenuItemImage* nextButton = CCMenuItemImage::create(
                                          "btn_next.png",
                                          "btn_next.png",
                                          this,
                                          menu_selector(LevelMenu::nextCallback));
    nextButton->setScale(visibleSize.height/nextButton->getContentSize().height * 0.11f);
    nextButton->setPosition(ccp(origin.x
                                 + visibleSize.width * 0.46f
                                 - menuButton->getContentSize().width * menuButton->getScaleX(),
                                 origin.y
                                 + visibleSize.height * 1.25f
                                 + nextButton->getContentSize().height/2 * nextButton->getScaleY()));
    nextButton->setTag(505);
    CCMenu* pNextMenu1 = CCMenu::create(nextButton, NULL);
    pNextMenu1->setPosition(CCPointZero);
    pNextMenu1->setTag(505);
    this->addChild(pNextMenu1, 1);
    
    CCMenuItemImage* replayButton = CCMenuItemImage::create(
                                            "btn_replay.png",
                                            "btn_replay.png",
                                            this,
                                            menu_selector(LevelMenu::replayCallback));
    replayButton->setScale(visibleSize.height/replayButton->getContentSize().height * 0.11f);
    replayButton->setPosition(ccp(origin.x
                                   + visibleSize.width * 0.54f
                                   + menuButton->getContentSize().width * menuButton->getScaleX(),
                                   origin.y
                                   + visibleSize.height * 1.25f
                                   + replayButton->getContentSize().height/2 * replayButton->getScaleY()));
    replayButton->setTag(504);
    CCMenu* pReplayMenu1 = CCMenu::create(replayButton, NULL);
    pReplayMenu1->setPosition(CCPointZero);
    pReplayMenu1->setTag(504);
    this->addChild(pReplayMenu1, 1);
    
    CCMenuItemImage* replayFramedButton = CCMenuItemImage::create(
                                                "btn_replay_framed.png",
                                                "btn_replay_framed_pressed.png",
                                                this,
                                                menu_selector(LevelMenu::replayCallback));
    replayFramedButton->setScale(visibleSize.height/menuFramedButton->getContentSize().height * 0.11f);
    replayFramedButton->setPosition(ccp(origin.x
                                         + visibleSize.width * 0.96f
                                         - replayFramedButton->getContentSize().width/2 * replayFramedButton->getScaleX()
                                         - menuFramedButton->getContentSize().width * menuFramedButton->getScaleX(),
                                         origin.y
                                         + visibleSize.height * 0.02f
                                         + replayFramedButton->getContentSize().height/2 * replayFramedButton->getScaleY()));
    replayFramedButton->setTag(507);
    CCMenu* pReplayMenu2 = CCMenu::create(replayFramedButton, NULL);
    pReplayMenu2->setPosition(CCPointZero);
    pReplayMenu2->setTag(507);
    this->addChild(pReplayMenu2, 1);
    
    _timeStarted = 0;
    _timeStopped = 0;

    if (Game::getInstance()->getMode() == Classic)
    {
        bindLevel();
    }
    else
    {
        CCFiniteTimeAction* actionDelay
        = CCDelayTime::create(0.05f);
        CCCallFuncN* action = CCCallFuncN::create(this,
                                               callfuncN_selector(LevelMenu::updateFreePlayDisplay));
        this->runAction(CCRepeatForever::create(CCSequence::create(actionDelay, action, NULL)));
    }
    
    CCMenuItemImage *muteButton = CCMenuItemImage::create(
                                                             "btn_mute_on.png",
                                                             "btn_mute_on_pressed.png",
                                                             this,
                                                             menu_selector(LevelMenu::muteButtonCallback));
	muteButton->setPosition(ccp(origin.x
                                + visibleSize.width
                                - muteButton->getContentSize().width/2 ,
                                origin.y
                                + muteButton->getContentSize().height/2));
    muteButton->setScale(visibleSize.height/muteButton->getContentSize().height * 0.11f);
    muteButton->setPosition(ccp(origin.x
                                 + visibleSize.width * 0.02f
                                 + muteButton->getContentSize().width/2 * muteButton->getScaleX(),
                                 origin.y
                                 + visibleSize.height * 0.02f
                                 + muteButton->getContentSize().height/2 * muteButton->getScaleY()));
    muteButton->setTag(1000);
    CCMenu* pMenuMute = CCMenu::create(muteButton, NULL);
    pMenuMute->setPosition(CCPointZero);
    pMenuMute->setTag(1000);
    this->addChild(pMenuMute, 1);
    
    if (SoundManager::getInstance()->isMute())
    {
        muteButton->setNormalImage(CCSprite::create("btn_mute_off.png"));
        muteButton->setSelectedImage(CCSprite::create("btn_mute_off_pressed.png"));
    }
    else
    {
        muteButton->setNormalImage(CCSprite::create("btn_mute_on.png"));
        muteButton->setSelectedImage(CCSprite::create("btn_mute_on_pressed.png"));
    }
    
    registerWithTouchDispatcher();
    
    EventHandler::getInstance()->setOnChangeBoardIndexListener(this);
    EventHandler::getInstance()->setOnLevelSolvedListener(this);
    EventHandler::getInstance()->setOnUnbindPiecesListener(this);
    
    return true;
}

void LevelMenu::beginLevel() {
    CCSize visibleSize = CCDirector::sharedDirector()->getVisibleSize();
    CCPoint origin = CCDirector::sharedDirector()->getVisibleOrigin();
    
    CCMenuItemImage* popupBg = (CCMenuItemImage*)this->getChildByTag(500)->getChildByTag(500);
    CCLabelTTF* beginText = (CCLabelTTF*)this->getChildByTag(501);
    
    //hide popup
    CCFiniteTimeAction* actionMove1 = CCMoveTo::create(0.15f, ccp(origin.x
                                                                + visibleSize.width * 0.5f,
                                                                origin.y
                                                                + visibleSize.height * 1.5f
                                                                + popupBg->getContentSize().height/2));
    
    popupBg->runAction(actionMove1);
    popupBg->setEnabled(false);
    
    CCFiniteTimeAction* actionMove2 = CCMoveTo::create(0.15f, ccp(origin.x
                                                                 + visibleSize.width * 0.5f,
                                                                 origin.y
                                                                 + visibleSize.height * 1.5f
                                                                 + beginText->getContentSize().height/2));
    beginText->runAction(actionMove2);
    
    //start timer
    _timeStarted = millisecondNow();
    
    //show level
    bindLevel();
}

void LevelMenu::bindLevel() {
    Level* level = Game::getInstance()->getLevel();

    for (int i = 0; i < Level::Height; i++) //y
    {
        for (int j = 0; j < Level::Width; j++) //x
        {
            int loc = i * Level::Width + j;
            int tileType = level->Board[loc];

            if (tileType != 0)
            {
                switch(tileType)
                {
                    case 1:
                    {

                        CCTexture2D* tex1 = CCTextureCache::sharedTextureCache()->addImage("tile_blue.png");
                        ((CCSprite*)this->getChildByTag(loc))->setTexture(tex1);
                        break;
                    }
                    case 2:
                    {
                        CCTexture2D* tex2 = CCTextureCache::sharedTextureCache()->addImage("tile_yellow.png");
                        ((CCSprite*)this->getChildByTag(loc))->setTexture(tex2);
                        break;
                    }
                    case 3:
                    {
                        CCTexture2D* tex3 = CCTextureCache::sharedTextureCache()->addImage("tile_purple.png");
                        ((CCSprite*)this->getChildByTag(loc))->setTexture(tex3);
                        break;
                    }
                    case 4:
                    {
                        CCTexture2D* tex4 = CCTextureCache::sharedTextureCache()->addImage("tile_red.png");
                        ((CCSprite*)this->getChildByTag(loc))->setTexture(tex4);
                        break;
                    }
                    case 5:
                    {
                        CCTexture2D* tex5 = CCTextureCache::sharedTextureCache()->addImage("tile_black.png");
                        ((CCSprite*)this->getChildByTag(loc))->setTexture(tex5);
                        break;
                    }
                }
                ((CCSprite*)this->getChildByTag(loc))->setOpacity(255);
            } else
            {
                ((CCSprite*)this->getChildByTag(loc))->setOpacity(0);
            }
        }
    }

    for (int i = 0; i < 8; i++)
    {
        if (i < level->Pieces->size())
        {
            int piece = level->Pieces->at(i)->getTile();
            
            switch(piece)
            {
                case L:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_L.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case R:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_R.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case U:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_U.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case D:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_D.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case LR:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_LR.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case LU:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_LU.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case LD:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_LD.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case RU:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_RU.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case RD:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_RD.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case UD:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_UD.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case LRU:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_LRU.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case LRD:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_LRD.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case LUD:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_LUD.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case RUD:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_RUD.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
                case LRUD:
                {
                    CCTexture2D* tex = CCTextureCache::sharedTextureCache()->addImage("piece_LRUD.png");
                    ((CCSprite*)this->getChildByTag(300+i))->setTexture(tex);
                    break;
                }
            }
            
            ((CCSprite*)this->getChildByTag(300+i))->setOpacity(255);
        }
        else
        {
            ((CCSprite*)this->getChildByTag(300+i))->setOpacity(0);
        }
    }

    EventHandler::getInstance()->onUnbindPieces();
}

void LevelMenu::beginButtonCallback(CCObject* pSender)
{
    beginLevel();
}

void LevelMenu::replayCallback(CCObject* pSender)
{
    if (Game::getInstance()->getMode() == Classic && Game::getInstance()->getLevel()->isSolved())
    {
        Game::getInstance()->setLevel(Game::getInstance()->getLevelId());
        CCScene *pScene = LevelMenu::scene();
        CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
    }
    else
    {
        Game::getInstance()->fullReset();
    }
}

void LevelMenu::nextCallback(CCObject* pSender)
{
    if (Game::getInstance()->nextLevel()) {
        CCScene *pScene = LevelMenu::scene();
        CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
    }
}

void LevelMenu::muteButtonCallback(CCObject* pSender)
{
    SoundManager::getInstance()->toggleMute();
    
    CCMenuItemImage* muteButton = (CCMenuItemImage*)this->getChildByTag(1000)->getChildByTag(1000);
    if (SoundManager::getInstance()->isMute())
    {
        muteButton->setNormalImage(CCSprite::create("btn_mute_off.png"));
        muteButton->setSelectedImage(CCSprite::create("btn_mute_off_pressed.png"));
    }
    else
    {
        muteButton->setNormalImage(CCSprite::create("btn_mute_on.png"));
        muteButton->setSelectedImage(CCSprite::create("btn_mute_on_pressed.png"));
    }
}

void LevelMenu::menuCallback(CCObject* pSender)
{
    if (Game::getInstance()->getMode() == Classic)
    {
        CCScene *pScene = ClassicMenu::scene();
        CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
    }
    else
    {
        CCScene *pScene = FreePlayMenu::scene();
        CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
    }
}

void LevelMenu::registerWithTouchDispatcher()
{
    CCDirector::sharedDirector()->getTouchDispatcher()->addTargetedDelegate(this, 0, true);
}

bool LevelMenu::ccTouchBegan(CCTouch* touch, CCEvent* event)
{
    CCPoint touchLocation = touch->getLocation();
    
    Level* level = Game::getInstance()->getLevel();
    
    _touchedPiece = -1;
    for (int i = 0; i < 8; i++)
    {
        if (i < level->Pieces->size())
        {
            if(((CCSprite*)this->getChildByTag(300+i))->boundingBox().containsPoint(touchLocation))
            {
                _touchedPiece = i;
                SoundManager::getInstance()->playClickSound();
                
                this->getChildByTag(999)->stopAllActions();
                Game::getInstance()->clearPiece(_touchedPiece);
            }
        }
    }
    
    return true;
}

void LevelMenu::ccTouchEnded(CCTouch* touch, CCEvent* event)
{
    if (_touchedPiece >= 0)
    {
        Level* level = Game::getInstance()->getLevel();
        
        CCPoint touchLocation = touch->getLocation();
        for (int i = 0; i < Level::Height; i++) //y
        {
            for (int j = 0; j < Level::Width; j++) //x
            {
                int loc = i * Level::Width + j;
                
                if (((CCSprite*)this->getChildByTag(loc))->boundingBox().containsPoint(touchLocation))
                {
                    bool pieceAlreadyHere = false;
                    for (int k = 0; k < level->Pieces->size(); k++)
                    {
                        Piece* piece = level->Pieces->at(k);
                        if (piece->placed)
                        {
                            if (piece->i == i && piece->j == j)
                            {
                                pieceAlreadyHere = true;
                            }
                        }
                    }
                    
                    if (!pieceAlreadyHere && (level->Board[loc] == 1 || level->Board[loc] == 4)) {
                        CCFiniteTimeAction* actionMove = CCMoveTo::create(0.1f,
                                                                          ((CCSprite*)this->getChildByTag(loc))->getPosition());
                        CCSprite* pieceSprite = ((CCSprite*)this->getChildByTag(300 + _touchedPiece));
                        pieceSprite->stopAllActions();
                        pieceSprite->runAction(actionMove);
                    
                        Game::getInstance()->setPiece(_touchedPiece, i, j);
                        _touchedPiece = -1;

                        CCFiniteTimeAction* actionDelay
                                = CCDelayTime::create(0.3f);
                        CCFiniteTimeAction* actionDelayDone
                                = CCCallFuncN::create(this,
                                                      callfuncN_selector(LevelMenu::delayFinished));
                        this->getChildByTag(999)->runAction(CCSequence::create(actionDelay, actionDelayDone, NULL) );

                        SoundManager::getInstance()->playClickSound();
                        return;
                    }
                }
            }
        }
        
        ((LevelMenu*)this)->UnbindPiece(_touchedPiece);
        Game::getInstance()->clearPiece(_touchedPiece);
        _touchedPiece = -1;
    }
}

void LevelMenu::delayFinished(CCNode* sender)
{
    Game::getInstance()->delayThenCheckForWin();
}

void LevelMenu::ccTouchCancelled(CCTouch* touch, CCEvent* event)
{
    if (_touchedPiece >= 0)
    {
        ((LevelMenu*)this)->UnbindPiece(_touchedPiece);
        Game::getInstance()->clearPiece(_touchedPiece);
    }
    
    _touchedPiece = -1;
}

void LevelMenu::ccTouchMoved(CCTouch* touch, CCEvent* event)
{
    if (_touchedPiece >= 0)
    {
        CCPoint touchLocation = touch->getLocation();
        ((CCSprite *)this->getChildByTag(300 + _touchedPiece))->setPosition(touchLocation);
    }
}

void IOnChangeBoardIndex::onChangeBoardIndex(int i, int j, int value)
{   
    int loc = i * Level::Width + j;
    
    switch(value)
    {
        case 1:
        {
            CCTexture2D* tex1 = CCTextureCache::sharedTextureCache()->addImage("tile_blue.png");
            ((CCSprite*)((LevelMenu*)this)->getChildByTag(loc))->setTexture(tex1);
            break;
        }
        case 2:
        {
            CCTexture2D* tex2 = CCTextureCache::sharedTextureCache()->addImage("tile_yellow.png");
            ((CCSprite*)((LevelMenu*)this)->getChildByTag(loc))->setTexture(tex2);
            break;
        }
        case 3:
        {
            CCTexture2D* tex3 = CCTextureCache::sharedTextureCache()->addImage("tile_purple.png");
            ((CCSprite*)((LevelMenu*)this)->getChildByTag(loc))->setTexture(tex3);
            break;
        }
        case 4:
        {
            CCTexture2D* tex4 = CCTextureCache::sharedTextureCache()->addImage("tile_red.png");
            ((CCSprite*)((LevelMenu*)this)->getChildByTag(loc))->setTexture(tex4);
            break;
        }
        case 5:
        {
            CCTexture2D* tex5 = CCTextureCache::sharedTextureCache()->addImage("tile_black.png");
            ((CCSprite*)((LevelMenu*)this)->getChildByTag(loc))->setTexture(tex5);
            break;
        }
    }
}

void IOnLevelSolved::onLevelSolved()
{
    CCSize visibleSize = CCDirector::sharedDirector()->getVisibleSize();
    CCPoint origin = CCDirector::sharedDirector()->getVisibleOrigin();
    
    CCMenuItemImage* popupBg = (CCMenuItemImage*)((LevelMenu*)this)->getChildByTag(500)->getChildByTag(500);
    CCLabelTTF* popupText = (CCLabelTTF*)((LevelMenu*)this)->getChildByTag(502);
    CCMenuItemImage* menuButton = (CCMenuItemImage*)((LevelMenu*)this)->getChildByTag(503)->getChildByTag(503);
    CCMenuItemImage* replayButton = (CCMenuItemImage*)((LevelMenu*)this)->getChildByTag(504)->getChildByTag(504);
    CCMenuItemImage* nextButton = (CCMenuItemImage*)((LevelMenu*)this)->getChildByTag(505)->getChildByTag(505);
    CCMenuItemImage* menuFramedButton = (CCMenuItemImage*)((LevelMenu*)this)->getChildByTag(506)->getChildByTag(506);
    CCMenuItemImage* replayFramedButton = (CCMenuItemImage*)((LevelMenu*)this)->getChildByTag(507)->getChildByTag(507);
    
    if (Game::getInstance()->getMode() == Classic)
    {   
        int levelId = Game::getInstance()->getLevelId();
        int nextLevel = levelId+1;
        bool unlockNext = (nextLevel < Level::Count);

        if (unlockNext)
        {
            SaveData::getInstance()->Unlock(nextLevel);
        }

        int halfDone = (Level::Count / 2) - 1;
        int allDone = Level::Count - 1;
        
        if (levelId == 4)
        {
            GPGSManager::UnlockAchievement("CgkIyMfpr_gNEAIQAg");
            //Firewall
        }
        else if (levelId == 9)
        {
            GPGSManager::UnlockAchievement("CgkIyMfpr_gNEAIQAw");
        }
        else if (levelId == halfDone)
        {
            GPGSManager::UnlockAchievement("CgkIyMfpr_gNEAIQBQ");
        }
        else if (levelId == 28)
        {
            GPGSManager::UnlockAchievement("CgkIyMfpr_gNEAIQBA");
        }
        else if (levelId == allDone)
        {
            GPGSManager::UnlockAchievement("CgkIyMfpr_gNEAIQAQ");
        }
        
        CCFiniteTimeAction* actionMoveMenuFramed = CCMoveTo::create(0.50f,
                                                              ccp(menuFramedButton->getPositionX(),
                                                                  origin.y - visibleSize.height));
        menuFramedButton->runAction(actionMoveMenuFramed);
        
        CCFiniteTimeAction* actionMoveReplayFramed = CCMoveTo::create(0.50f,
                                                                    ccp(replayFramedButton->getPositionX(),
                                                                        origin.y - visibleSize.height));
        replayFramedButton->runAction(actionMoveReplayFramed);
        
        CCFiniteTimeAction* actionMoveBg = CCMoveTo::create(0.15f,
                                                            ccp(origin.x + visibleSize.width/2,
                                                                origin.y + visibleSize.height/2));
        popupBg->runAction(actionMoveBg);
        
        CCFiniteTimeAction* actionMoveText = CCMoveTo::create(0.15f,
                                                            ccp(origin.x
                                                                + visibleSize.width * 0.5f,
                                                                origin.y
                                                                + visibleSize.height * 0.55f));
        popupText->runAction(actionMoveText);
        
        CCFiniteTimeAction* actionMoveMenu = CCMoveTo::create(0.15f,
                                                                ccp(menuButton->getPositionX(),
                                                                    origin.y
                                                                    + visibleSize.height * 0.25f
                                                                    + menuButton->getContentSize().height/2 * menuButton->getScaleY()));
        menuButton->runAction(actionMoveMenu);
        
        CCFiniteTimeAction* actionMoveReplay = CCMoveTo::create(0.15f,
                                                                ccp(replayButton->getPositionX(),
                                                                    origin.y
                                                                    + visibleSize.height * 0.25f
                                                                    + replayButton->getContentSize().height/2 * replayButton->getScaleY()));
        replayButton->runAction(actionMoveReplay);
        
        if (unlockNext)
        {
            CCFiniteTimeAction* actionMoveNext = CCMoveTo::create(0.15f,
                                                                    ccp(nextButton->getPositionX(),
                                                                        origin.y
                                                                        + visibleSize.height * 0.25f
                                                                        + nextButton->getContentSize().height/2 * nextButton->getScaleY()));
            nextButton->runAction(actionMoveNext);
        }
    }
    else if (Game::getInstance()->getMode() == FreePlay)
    {
        if (Game::getInstance()->nextFreePlayLevel())
        {
            
            ((LevelMenu *)this)->bindLevel();
        }
        else
        {
            _timeStopped = ((LevelMenu*)this)->millisecondNow();
            int duration = _timeStopped - _timeStarted;
            int mins = (duration / (1000 * 60)) % 60;
            int secs = (duration / 1000) % 60;
            int millis = duration % 1000;
            
            std::stringstream ss;
            ss << "COMPLETED IN: " << std::endl;
            if (mins > 0)
            {
                ss << ((LevelMenu*)this)->convertInt(mins) << ":";
            }
            if (secs < 10)
            {
                ss << "0";
            }
            ss << ((LevelMenu*)this)->convertInt(secs) << ":";
            if (millis < 100)
            {
                ss << "0";
            }
            if (millis < 10)
            {
                ss << "0";
            }
            ss << ((LevelMenu*)this)->convertInt(millis) << " ";
            popupText->setString(ss.str().c_str());
            
            CCFiniteTimeAction* actionMoveBg = CCMoveTo::create(0.15f,
                                                                ccp(origin.x + visibleSize.width/2,
                                                                    origin.y + visibleSize.height/2));
            popupBg->runAction(actionMoveBg);
            
            CCFiniteTimeAction* actionMoveText = CCMoveTo::create(0.15f,
                                                                  ccp(origin.x
                                                                      + visibleSize.width * 0.5f,
                                                                      origin.y
                                                                      + visibleSize.height * 0.55f));
            popupText->runAction(actionMoveText);
            
            CCFiniteTimeAction* actionMoveMenu = CCMoveTo::create(0.15f,
                                                                  ccp(menuButton->getPositionX(),
                                                                      origin.y
                                                                      + visibleSize.height * 0.25f
                                                                      + menuButton->getContentSize().height/2 * menuButton->getScaleY()));
            menuButton->runAction(actionMoveMenu);
            
            Difficulty difficulty = Game::getInstance()->getDifficulty();
            
            int currentBestScore = SaveData::getInstance()->GetBestTime(difficulty);
            
            if (currentBestScore == 0 || currentBestScore > duration)
            {
                SaveData::getInstance()->SetBestTime(difficulty, duration);
            }
            SaveData::getInstance()->IncrementFreePlayCount(difficulty);
            
            if (GPGSManager::IsSignedIn())
            {
                if (difficulty == Beginner)
                {
                    GPGSManager::SubmitHighScore("CgkIyMfpr_gNEAIQBw", duration);
                }
                else if (difficulty == Easy)
                {
                    GPGSManager::SubmitHighScore("CgkIyMfpr_gNEAIQCA", duration);
                }
                else if (difficulty == Medium)
                {
                    GPGSManager::SubmitHighScore("CgkIyMfpr_gNEAIQCQ", duration);
                }
                else if (difficulty == Hard)
                {
                    GPGSManager::SubmitHighScore("CgkIyMfpr_gNEAIQCg", duration);
                }
                else if (difficulty == Challenging)
                {
                    GPGSManager::SubmitHighScore("CgkIyMfpr_gNEAIQCw", duration);
                }
            }
        }
    }
}

void IOnUnbindPieces::onUnbindPieces()
{
    for (int i = 0; i < 8; i++)
    {
        ((LevelMenu*)this)->UnbindPiece(i);
    }
}

void LevelMenu::UnbindPiece(int i)
{
    CCSize visibleSize = CCDirector::sharedDirector()->getVisibleSize();
    CCPoint origin = CCDirector::sharedDirector()->getVisibleOrigin();
    CCSprite* pieceSprite = ((CCSprite *)this->getChildByTag(300 + i));
    
    float bugTrayY = origin.y
            + visibleSize.height * 0.075f;
    float pieceWidth = pieceSprite->getContentSize().width * pieceSprite->getScale() * 1.1f;
    float bugX = origin.x
            + visibleSize.width / 2
            + (pieceWidth * (float)((float)i - 3.0f) * 1.1f);
    CCFiniteTimeAction* actionMove = CCMoveTo::create(0.15f, ccp(bugX, bugTrayY));
    pieceSprite->stopAllActions();
    pieceSprite->runAction(actionMove);
}

void LevelMenu::updateFreePlayDisplay() {
    
    std::stringstream ss;
    
    switch (Game::getInstance()->getDifficulty())
    {
        case Beginner:
            ss << "Beginner";
            break;
        case Easy:
            ss << "Easy";
            break;
        case Medium:
            ss << "Medium";
            break;
        case Hard:
            ss << "Hard";
            break;
        case Challenging:
            ss << "Challenging";
            break;
    }
    
    ss << "   ";
    ss << Game::getInstance()->getLevelIndex() + 1;
    ss << "/5   ";
    
    if (_timeStarted != 0)
    {
        int duration = _timeStopped == 0
                ? ((LevelMenu*)this)->millisecondNow() - _timeStarted
                : _timeStopped - _timeStarted;
        int mins = (duration / (1000 * 60)) % 60;
        int secs = (duration / 1000) % 60;
        int millis = duration % 1000;
        if (mins > 0)
        {
            ss << convertInt(mins) << ":";
        }
        if (secs < 10)
        {
            ss << "0";
        }
        ss << convertInt(secs) << ":";
        if (millis < 100)
        {
            ss << "0";
        }
        if (millis < 10)
        {
            ss << "0";
        }
        ss << convertInt(millis) << " ";
        
        //CHEAT-PROOF: if duration < 0, drop to menu.
        if (duration < 0) {
            CCScene *pScene = FreePlayMenu::scene();
            CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
        }
    }
    
    const char *statString = ss.str().c_str();
    
    CCLabelTTF* levelText = (CCLabelTTF*)((LevelMenu*)this)->getChildByTag(400);
    float originalWidth = levelText->getContentSize().width;
    levelText->setString(statString);
    float newWidth = levelText->getContentSize().width;
    levelText->setPositionX(levelText->getPositionX() + ((newWidth - originalWidth) / 2));
}

std::string LevelMenu::convertInt(int number)
{
    std::stringstream ss;//create a stringstream
    ss << number;//add number to the stream
    return ss.str();//return a string with the contents of the stream
}

long LevelMenu::millisecondNow()
{
    struct cc_timeval now;
    CCTime::gettimeofdayCocos2d(&now, NULL);
    return (now.tv_sec * 1000 + now.tv_usec / 1000);
}