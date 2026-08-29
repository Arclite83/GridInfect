#include "ClassicMenuScene.h"
#include "Game.h"
#include "Level.h"
#include "LevelMenuScene.h"
#include "MainMenuScene.h"
#include "SaveData.h"
#include "SoundManager.h"

USING_NS_CC;

CCSize _visibleSize;
int _animatedSpriteCount = 0;

CCScene* ClassicMenu::scene()
{
    // 'scene' is an autorelease object
    CCScene *scene = CCScene::create();
    
    // 'layer' is an autorelease object
    ClassicMenu *layer = ClassicMenu::create();

    // add layer as a child to scene
    scene->addChild(layer);

    // return the scene
    return scene;
}

// on "init" you need to initialize your instance
bool ClassicMenu::init()
{
    _animatedSpriteCount = 0;
    if ( !CCLayer::init() )
    {
        return false;
    }
    
    _visibleSize = CCDirector::sharedDirector()->getVisibleSize();
    CCPoint origin = CCDirector::sharedDirector()->getVisibleOrigin();

    CCSprite* backgroundSprite = CCSprite::create("background.png");
    backgroundSprite->setPosition(ccp(_visibleSize.width/2 + origin.x, _visibleSize.height/2 + origin.y));
    backgroundSprite->setScaleX(_visibleSize.width/backgroundSprite->getContentSize().width);
    backgroundSprite->setScaleY(_visibleSize.height/backgroundSprite->getContentSize().height);
    this->addChild(backgroundSprite, 0);
    
    int buttons_on_page = 32;
    for (int i = 0; i < Level::Count; i++) {
        int temp_i = i;
        float y = 0;
        float yOffset = _visibleSize.height * 0.12f;
        float xOffset = _visibleSize.width * 0.02f;

        while (temp_i >= buttons_on_page) {
            temp_i -= buttons_on_page;
            y += _visibleSize.height;
        }
        
        y += (yOffset + (temp_i / 8) * _visibleSize.height * 0.19f);
        float x = xOffset + ((temp_i % 8) * _visibleSize.width * 0.11f);

        bool isUnlocked = SaveData::getInstance()->IsUnlocked(i);
        
        CCMenuItemImage *levelButton = CCMenuItemImage::create(
                                                               "level_box.png",
                                                               "level_box_pressed.png",
                                                               this,
                                                               menu_selector(ClassicMenu::goToLevelCallback));
        levelButton->setScale(_visibleSize.width/levelButton->getContentSize().width * 0.10f);
        levelButton->setPosition(ccp(origin.x
                                     + x
                                     + levelButton->getContentSize().width/2 * levelButton->getScaleX(),
                                     origin.y + _visibleSize.height
                                     - y
                                     - levelButton->getContentSize().height/2 * levelButton->getScaleY()
                                     + _visibleSize.height * Game::getInstance()->getClassicMenuPage()));
        levelButton->setTag(i);
        CCMenu* pMenuLevel = CCMenu::create(levelButton, NULL);
        pMenuLevel->setPosition(CCPointZero);
        pMenuLevel->setTag(_animatedSpriteCount++);
        this->addChild(pMenuLevel, 1);
        
        std::stringstream s;
        s << (i + 1);
        CCLabelTTF* text = CCLabelTTF::create(s.str().c_str(),
                                              "fonts/Overhaul.ttf",
                                              _visibleSize.height * 0.085f);
        text->cocos2d::CCNodeRGBA::setColor(ccBLACK);
        text->setPosition(ccp(origin.x
                              + x
                              + levelButton->getContentSize().width * 0.5f * levelButton->getScaleX(),
                              origin.y + _visibleSize.height
                              - y
                              - levelButton->getContentSize().height * 0.5f * levelButton->getScaleY()
                              + _visibleSize.height * Game::getInstance()->getClassicMenuPage()
                              - text->getContentSize().height * 0.1f));
        text->setTag(_animatedSpriteCount++);
        this->addChild(text, 2);
        
        text->setOpacity(255);
        
        if (isUnlocked || i == 0)
        {
            levelButton->setEnabled(true);

        }
        else
        {
            levelButton->setEnabled(false);
            
            CCSprite* padlock = CCSprite::create("padlock.png");
            padlock->setPosition(ccp(origin.x
                                     + x
                                     + levelButton->getContentSize().width/2 * levelButton->getScaleX(),
                                     origin.y + _visibleSize.height
                                     - y
                                     - levelButton->getContentSize().height/2 * levelButton->getScaleY()
                                     + _visibleSize.height * Game::getInstance()->getClassicMenuPage()));
            padlock->setScale(_visibleSize.width/levelButton->getContentSize().width * 0.15f);
            padlock->setTag(_animatedSpriteCount++);
            padlock->setOpacity(192);
            this->addChild(padlock, 3);
        }
    }
    
    CCMenuItemImage *downButton = CCMenuItemImage::create(
                                                          "btn_down.png",
                                                          "btn_down_pressed.png",
                                                          this,
                                                          menu_selector(ClassicMenu::pageDownCallback));
    downButton->setScale(_visibleSize.width/downButton->getContentSize().width * 0.07f);
    downButton->setPosition(ccp(origin.x
                                + _visibleSize.width * 0.98f
                                - downButton->getContentSize().width/2 * downButton->getScaleX(),
                                origin.y
                                + _visibleSize.height * 0.49f
                                - downButton->getContentSize().height/2 * downButton->getScaleY()));
    CCMenu* pMenuDown = CCMenu::create(downButton, NULL);
    pMenuDown->setPosition(CCPointZero);
    this->addChild(pMenuDown, 1);
    
    CCMenuItemImage *upButton = CCMenuItemImage::create(
                                                        "btn_up.png",
                                                        "btn_up_pressed.png",
                                                        this,
                                                        menu_selector(ClassicMenu::pageUpCallback));
    upButton->setScale(_visibleSize.width/upButton->getContentSize().width * 0.07f);
    upButton->setPosition(ccp(origin.x
                              + _visibleSize.width * 0.98f
                              - upButton->getContentSize().width/2 * upButton->getScaleX(),
                              origin.y
                              + _visibleSize.height * 0.51f
                              + upButton->getContentSize().height/2 * upButton->getScaleY()));
    CCMenu* pMenuUp = CCMenu::create(upButton, NULL);
    pMenuUp->setPosition(CCPointZero);
    this->addChild(pMenuUp, 1);
    
    CCMenuItemImage *homeButton = CCMenuItemImage::create(
                                                           "btn_home_framed.png",
                                                           "btn_home_framed_pressed.png",
                                                           this,
                                                           menu_selector(ClassicMenu::homeButtonCallback));
    homeButton->setScale(_visibleSize.height/homeButton->getContentSize().height * 0.11f);
    homeButton->setPosition(ccp(origin.x
                                 + _visibleSize.width * 0.98f
                                 - homeButton->getContentSize().width/2 * homeButton->getScaleX(),
                                 origin.y
                                 + _visibleSize.height * 0.02f
                                 + homeButton->getContentSize().height/2 * homeButton->getScaleY()));
    CCMenu* pMenuHome = CCMenu::create(homeButton, NULL);
    pMenuHome->setPosition(CCPointZero);
    this->addChild(pMenuHome, 1);
    
    CCMenuItemImage *muteButton = CCMenuItemImage::create(
                                                          "btn_mute_on.png",
                                                          "btn_mute_on_pressed.png",
                                                          this,
                                                          menu_selector(ClassicMenu::muteButtonCallback));
    
    
    
	muteButton->setPosition(ccp(origin.x
                                + _visibleSize.width
                                - muteButton->getContentSize().width/2 ,
                                origin.y
                                + muteButton->getContentSize().height/2));
    muteButton->setScale(_visibleSize.height/muteButton->getContentSize().height * 0.11f);
    muteButton->setPosition(ccp(origin.x
                                + _visibleSize.width * 0.02f
                                + muteButton->getContentSize().width/2 * muteButton->getScaleX(),
                                origin.y
                                + _visibleSize.height * 0.02f
                                + muteButton->getContentSize().height/2 * muteButton->getScaleY()));
    muteButton->setTag(1000);
    CCMenu* pMenuMute = CCMenu::create(muteButton, NULL);
    pMenuMute->setPosition(CCPointZero);
    pMenuMute->setTag(1000);
    this->addChild(pMenuMute, 5);
    
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
    
    return true;
}

void ClassicMenu::homeButtonCallback(CCObject* pSender)
{
    CCScene *pScene = MainMenu::scene();
    CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
}

void ClassicMenu::muteButtonCallback(CCObject* pSender)
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

void ClassicMenu::goToLevelCallback(CCObject* pSender)
{
    CCMenuItemImage *button = (CCMenuItemImage *)pSender;
    if (button != NULL) {
        int levelId = button->getTag();
        Game::getInstance()->setLevel(levelId);
        CCScene *pScene = LevelMenu::scene();
        CCDirector::sharedDirector()->replaceScene(CCTransitionFade::create(0.5f, pScene));
    }
}

void ClassicMenu::pageUpCallback(CCObject* pSender)
{
    int page = Game::getInstance()->getClassicMenuPage();
    if (page > 0) {
        Game::getInstance()->setClassicMenuPage(page - 1);

        float duration = 0.2f;
        for (int i = 0; i < _animatedSpriteCount; i++)
        {
            CCNodeRGBA *sprite = (CCNodeRGBA *)this->getChildByTag(i);
            if (sprite != NULL)
            {
                CCFiniteTimeAction* actionMove = CCMoveTo::create((float)duration,
                                                                  ccp(sprite->getPositionX(),
                                                                      sprite->getPositionY() - _visibleSize.height));
                sprite->runAction(actionMove);
            }
        }
    }
}

void ClassicMenu::pageDownCallback(CCObject* pSender)
{
    int page = Game::getInstance()->getClassicMenuPage();
    int numPages = ((Level::Count - 1) / 32);
    if (page < numPages) {
        Game::getInstance()->setClassicMenuPage(page + 1);

        float duration = 0.2f;
        for (int i = 0; i < _animatedSpriteCount; i++)
        {
            CCNodeRGBA *sprite = (CCNodeRGBA *)this->getChildByTag(i);
            if (sprite != NULL)
            {
                CCFiniteTimeAction* actionMove = CCMoveTo::create((float)duration,
                                                                  ccp(sprite->getPositionX(),
                                                                      sprite->getPositionY() + _visibleSize.height));
            
                sprite->runAction(actionMove);
            }
        }
    }
}