package com.example.giftofthegivers_mobile.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.giftofthegivers_mobile.data.ReliefProject
import com.example.giftofthegivers_mobile.ui.theme.Accent
import com.example.giftofthegivers_mobile.ui.theme.PrimarySubtle

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DonateScreen(
    projects: List<ReliefProject>,
    preselectedProjectId: String?,
    onSubmitDonation: (projectId: String, amount: Double, currency: String, donorName: String, email: String, consent: Boolean) -> Unit
) {
    var selectedProjectId by remember { mutableStateOf(preselectedProjectId ?: projects.firstOrNull()?.id ?: "") }
    var amountText by remember { mutableStateOf("500") }
    var selectedCurrency by remember { mutableStateOf("ZAR") }
    var donorName by remember { mutableStateOf("John Doe") }
    var donorEmail by remember { mutableStateOf("donor@example.com") }
    var consentGiven by remember { mutableStateOf(true) }
    var showError by remember { mutableStateOf(false) }

    val exchangeRate = when (selectedCurrency) {
        "USD" -> 18.50
        "EUR" -> 20.00
        else -> 1.00
    }
    val numericAmount = amountText.toDoubleOrNull() ?: 0.0
    val calculatedZar = numericAmount * exchangeRate

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Make a Donation", fontWeight = FontWeight.Bold) }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .padding(16.dp)
        ) {
            Text(
                text = "Support Humanitarian Relief",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
            )
            Text(
                text = "100% transparent tracking. Secure payment via PayFast sandbox.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )

            Spacer(modifier = Modifier.height(16.dp))

            Text(text = "Select Relief Project", fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
            Spacer(modifier = Modifier.height(6.dp))

            projects.forEach { project ->
                val isSelected = project.id == selectedProjectId
                Card(
                    onClick = { selectedProjectId = project.id },
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 4.dp),
                    colors = CardDefaults.cardColors(
                        containerColor = if (isSelected) PrimarySubtle else MaterialTheme.colorScheme.surface
                    ),
                    border = if (isSelected) ButtonDefaults.outlinedButtonBorder else null
                ) {
                    Column(modifier = Modifier.padding(12.dp)) {
                        Text(text = project.title, fontWeight = FontWeight.Bold, fontSize = 14.sp)
                        Text(text = project.location, fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                OutlinedTextField(
                    value = amountText,
                    onValueChange = { amountText = it },
                    label = { Text("Amount") },
                    modifier = Modifier.weight(1f),
                    singleLine = true
                )

                val currencies = listOf("ZAR", "USD", "EUR")
                Row(
                    modifier = Modifier
                        .align(Alignment.CenterVertically)
                        .background(PrimarySubtle, RoundedCornerShape(8.dp))
                        .padding(4.dp)
                ) {
                    currencies.forEach { cur ->
                        val selected = cur == selectedCurrency
                        Button(
                            onClick = { selectedCurrency = cur },
                            colors = ButtonDefaults.buttonColors(
                                containerColor = if (selected) MaterialTheme.colorScheme.primary else Color.Transparent,
                                contentColor = if (selected) Color.White else MaterialTheme.colorScheme.onSurface
                            ),
                            elevation = null,
                            modifier = Modifier.height(40.dp),
                            contentPadding = PaddingValues(horizontal = 12.dp, vertical = 0.dp)
                        ) {
                            Text(cur, fontSize = 12.sp, fontWeight = FontWeight.Bold)
                        }
                    }
                }
            }

            if (selectedCurrency != "ZAR") {
                Spacer(modifier = Modifier.height(6.dp))
                Text(
                    text = "Indicative equivalent: R %,.2f (at rate 1 $selectedCurrency = R $exchangeRate)".format(calculatedZar),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.primary,
                    fontWeight = FontWeight.SemiBold
                )
            }

            Spacer(modifier = Modifier.height(16.dp))

            OutlinedTextField(
                value = donorName,
                onValueChange = { donorName = it },
                label = { Text("Donor Name (Optional - leave blank for Anonymous)") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )

            Spacer(modifier = Modifier.height(12.dp))

            OutlinedTextField(
                value = donorEmail,
                onValueChange = { donorEmail = it },
                label = { Text("Email Address") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )

            Spacer(modifier = Modifier.height(16.dp))

            Row(
                verticalAlignment = Alignment.CenterVertically,
                modifier = Modifier.fillMaxWidth()
            ) {
                Checkbox(
                    checked = consentGiven,
                    onCheckedChange = { consentGiven = it }
                )
                Text(
                    text = "I consent to processing of personal data under POPIA privacy policy.",
                    style = MaterialTheme.typography.bodySmall
                )
            }

            if (showError) {
                Spacer(modifier = Modifier.height(8.dp))
                Text(
                    text = "Please enter a valid amount and check the POPIA consent box.",
                    color = MaterialTheme.colorScheme.error,
                    style = MaterialTheme.typography.bodySmall
                )
            }

            Spacer(modifier = Modifier.height(24.dp))

            Button(
                onClick = {
                    if (numericAmount <= 0 || !consentGiven) {
                        showError = true
                    } else {
                        showError = false
                        onSubmitDonation(selectedProjectId, numericAmount, selectedCurrency, donorName, donorEmail, consentGiven)
                    }
                },
                modifier = Modifier
                    .fillMaxWidth()
                    .height(50.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Accent),
                shape = RoundedCornerShape(12.dp)
            ) {
                Text(
                    text = "CONTINUE TO PAYMENT",
                    fontSize = 15.sp,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onSecondary
                )
            }

            Spacer(modifier = Modifier.height(32.dp))
        }
    }
}
