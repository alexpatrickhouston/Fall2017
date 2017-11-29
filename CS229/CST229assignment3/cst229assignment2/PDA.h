#pragma once
//DFA.h
//represents a dfa
#include "stdafx.h"
#include <string>
#include <list>
#include <set>
#include <map>

using namespace std;

class PDA
{
private:
	enum ACTION_TYPE {READ,PUSH,POP};
	struct Action
	{
		ACTION_TYPE type;
		char dataChar;
		Action(ACTION_TYPE T, char dC);
	};

	char initialState;
	char finalState;
	map<string, char> transitions;
	map<char, Action> actions;

public:
	PDA(char initialState);
	~PDA();
	void AddTransistion(char currentState, string action, char dataChar, char nextState);
	void SetFinalState(char finalState);
	bool Accept(string s);
private:
	string MakeKey(char currentState, char inputChar );

};