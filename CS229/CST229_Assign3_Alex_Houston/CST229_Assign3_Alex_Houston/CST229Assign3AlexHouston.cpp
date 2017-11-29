// CST229Assign3AlexHouston.cpp : Defines the entry point for the console application.
//
#include "stdafx.h"
#include "PDA.h"
#include <string>
#include <list>
#include <iostream>
#include <vector>
using namespace std;
int main()
{
	list<string> transitions;
	list<string> inputStrings;
	string line;
	std::getline(cin, line);
	std::getline(std::cin, line);
	std::getline(std::cin, line);
	while (line.find(',') != -1)//find a comma
	{
		transitions.push_back(line);
		std::getline(std::cin, line);
	}
	do
	{
		inputStrings.push_back(line);
	} while (getline(cin, line));
	//read input file

	PDA pda(transitions.front()[0]);
	for each(string t in transitions)
	{
		//each transistion includes current state, action, data char, next state
		//S, read, a ,A
		vector<string> parts = SplitString(t);
		pda.AddTransition(parts[0][0], parts[1], parts[2][0], parts[3][0]);

	}

		pda.SetFinalState('F');
	
	//create DFA
	for each (std::string s in inputStrings)
	{
		if (pda.Accept(s))
		{
			cout << "Accept: \t" << s << endl;
		}
		else
		{
			cout << "Not Accepted:<<" << s << endl;
		}
	}
	//process each string and print whether accept or reject

	return 0;
}
std::vector <std::string> SplitString(std::string input)
{
	vector<string> result;
	int i = input.find(", ");
	if (i >= 0)
	{
		result.push_back(input.substr(0, i));
		input = input.substr(i+1);
	}
	result.push_back(input);
	return result;
}

