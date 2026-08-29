package com.bloodhoundstudios.gridinfect;

import android.widget.Toast;

import com.google.android.gms.games.Games;

public class GameCenter {
	
	static boolean isSignedIn() {
		try {
			if (GridInfect.Instance != null) {
	    		return GridInfect.Instance.isSignedIn();
			}
		} catch (Exception ex) {
		}
		return false;
	}
    
    static void signIn() {
    	try {
	    	if (GridInfect.Instance != null) {
	    		if (!GridInfect.Instance.isSignedIn()) {
	    			GridInfect.Instance.beginUserInitiatedSignIn();
	    		} else {
	    			GridInfect.Instance.signOut();
	    			Toast.makeText(GridInfect.Instance, "Signed out", Toast.LENGTH_SHORT).show();
	    		}
	    	}
    	} catch (Exception ex) {
    	}
    }
    
	static void showAchievements() {
		try {
	    	if (GridInfect.Instance != null) {
	    		if (GridInfect.Instance.isSignedIn()) {
	    			GridInfect.Instance.startActivityForResult(
	    					Games.Achievements.getAchievementsIntent(
	    							GridInfect.Instance.getApiClient()), 
	    							41267);
	    		} else {
	    			GridInfect.Instance.beginUserInitiatedSignIn();
	    		}
	    	}
    	} catch (Exception ex) {
    	}
	}
	
    static void unlockAchievement(String achievementId) {
    	try {
	    	if (GridInfect.Instance != null) {
	    		if (GridInfect.Instance.isSignedIn()) {
	    			Games.Achievements.unlock(GridInfect.Instance.getApiClient(), achievementId);
	    		}
	    	}
    	} catch (Exception ex) {
    	}
    }
    
    static void showLeaderboard(String leaderboardId) {
		try {
	    	if (GridInfect.Instance != null) {
	    		if (GridInfect.Instance.isSignedIn()) {
	    			GridInfect.Instance.startActivityForResult(
	    					Games.Leaderboards.getLeaderboardIntent(
	    							GridInfect.Instance.getApiClient(), leaderboardId),
	    							76789);
	    		} else {
	    			GridInfect.Instance.beginUserInitiatedSignIn();
	    		}
	    	}
    	} catch (Exception ex) {
    	}
    }
    
    static void postToLeaderboard(String leaderboardId, int score) {
		try {
	    	if (GridInfect.Instance != null) {
	    		if (GridInfect.Instance.isSignedIn()) {
	    			Games.Leaderboards.submitScore(GridInfect.Instance.getApiClient(), leaderboardId, score);
	    		}
	    	}
    	} catch (Exception ex) {
    	}
    }
}
