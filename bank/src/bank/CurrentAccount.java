
package bank;

public class CurrentAccount extends Account {
    private double overdraftLimit;

    public CurrentAccount(String accountNumber, String accountHolderName, double initialBalance, double overdraftLimit) {
        super(accountNumber, accountHolderName, initialBalance);
        this.overdraftLimit = overdraftLimit;
    }

    @Override
    public void deposit(double amount) {
        if (amount > 0) {
            balance += amount;
            addTransaction("DEPOSIT", amount, balance);
            System.out.println("Deposited $" + amount + " to Current Account");
        }
    }

    @Override
    public boolean withdraw(double amount) {
        if (amount > 0 && (balance - amount) >= -overdraftLimit) {
            balance -= amount;
            addTransaction("WITHDRAWAL", amount, balance);
            System.out.println("Withdrew $" + amount + " from Current Account");
            return true;
        }
        else{
        System.out.println("Withdrawal failed! Overdraft limit exceeded.");
        return false;
        }
    }

    @Override
    public double calculateInterest() {
        return 0; // No interest for current account
    }

    @Override
    public String toString() {
        return "Current Account - Balance: $" + String.format("%.2f", balance) + 
               ", Overdraft Limit: $" + overdraftLimit;
    }
}
