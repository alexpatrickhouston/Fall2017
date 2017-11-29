#pragma once
#include "stdafx.h"
#include <stack>
#include <set>
#include <map>


using namespace std;
class PDA
{
private:
	enum ACTION_TYPE{READ, PUSH, POP};
	struct Action
	{
		ACTION_TYPE type;
		char DataChar;
		Action(ACTION_TYPE t, char Dc);
	};
	char initialState;//starting position of pda
	char finalState;//final state of pda
	map<string, char> transitions;//map of transitions
	map<char, Action> actions;//map of actions




public:
	PDA(char initialstate);
	~PDA();
	void AddTransition(char currentState, string action, char inputChar, char nextState);
	bool Accept(string s);
	void SetFinalState(char finalState);

private:
	string MakeKey(char currentState, char datachar);
};

