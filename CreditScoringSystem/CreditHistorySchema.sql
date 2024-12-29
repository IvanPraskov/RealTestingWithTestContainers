CREATE TABLE Customers (
	CustomerId NCHAR(10) PRIMARY KEY,
	DateOfBirth DATE NOT NULL,
	CustomerFirstName VARCHAR(50) NOT NULL,
	CustomerMiddleName VARCHAR(50) NULL,
	CustomerLastName VARCHAR(50) NOT NULL
);

CREATE TABLE CreditRequestScoringResults(
	CreditRequestScoringResultId SERIAL PRIMARY KEY,
	CustomerId NCHAR(10) NOT NULL REFERENCES Customers(CustomerId),
	RequestedAmount DECIMAL (14,2) NOT NULL,
	ScoringResultDecisionId INT NOT NULL,
	Score INT NOT NULL,
	CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
	);
	
CREATE TABLE CreditHistories(
	CreditHistoryId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
	CustomerId NCHAR(10) NOT NULL REFERENCES Customers(CustomerId),
	MissedPayments INT NOT NULL DEFAULT 0
);