#include "stdafx.h"
#include "PDA.h"
#include <string>

using namespace std;

PDA::PDA(char initialstate)
{
	this->initialState = initialstate;
	finalState = '\0';
}

PDA::~PDA()
{

}
string PDA::MakeKey(char currentState, char datachar)
{
	string key;
	key += currentState;
	key += datachar;
	return key;
}
void PDA::AddTransition(char currentState, string action, char inputChar, char nextState)
{
	//make a unique key out of the currentstate and datachar
	string key = MakeKey(currentState, inputChar);
	//add it to the list of transitions with the next state as the value of the key
	transitions[key] = nextState;
	ACTION_TYPE at = READ;
	if (action == "push")
		at = PUSH;
	else if (action == "pop")
		at = POP;
	else if (action == "read")
		at = READ;
	else
		throw new exception("invalid action");
	actions[currentState] = Action(at,inputChar);


}
bool PDA::Accept(string s)
{
	return false;
}
void PDA::SetFinalState(char finalState)
{
	this->finalState = finalState;
}
