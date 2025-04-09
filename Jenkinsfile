pipeline {
    agent any
    
    environment {
        PROJECT_NAME = 'ProNet.Api'
        SOLUTION_FILE = 'Api.csproj'
        PUBLISH_DIR = 'publish'
        GITHUB_REPO = 'https://github.com/shrifzain/ApiNet.git'
        S3_BUCKET = 'pronet-artifacts'
        BLUE_SERVER = '54.152.102.169'           // Blue instance IP
        GREEN_SERVER = '54.158.110.100'          // Green instance IP
        SSH_KEY_ID = 'sheraa-ssh-key'            // Jenkins credential ID for SSH key
        AWS_KEY_ID = 'awss'                      // AWS credential ID
        ACTIVE_ENV = 'blue'                      // Initial active environment
        TARGET_SERVER = ''                       // Placeholder for target server, set later
    }
    
    stages {
        stage('Get Code') {
            steps {
                echo 'Getting code from GitHub'
                git url: "${GITHUB_REPO}", branch: 'master', credentialsId: '01143717'
                sh 'ls -la'
                sh 'find . -type f'
                sh 'find . -name "*.csproj"'
            }
        }
        
        stage('Build') {
            steps {
                echo 'Building the app'
                sh "dotnet publish ${SOLUTION_FILE} -c Release -o ${PUBLISH_DIR}"
            }
        }
        
        stage('Save to S3') {
            steps {
                echo 'Saving app to S3'
                withAWS(credentials: "${AWS_KEY_ID}") {
                    sh "aws s3 cp ${PUBLISH_DIR} s3://${S3_BUCKET}/${PROJECT_NAME}/ --recursive"
                }
            }
        }
        
        stage('Choose Inactive Server') {
            steps {
                echo 'Choosing inactive environment'
                sh '''
                    set -e  # Exit on error
                    echo "ACTIVE_ENV is: $ACTIVE_ENV"
                    if [ "$ACTIVE_ENV" = "blue" ]; then
                        echo "Selected inactive environment: green (${GREEN_SERVER})"
                        echo "${GREEN_SERVER}" > target_server.txt
                    else
                        echo "Selected inactive environment: blue (${BLUE_SERVER})"
                        echo "${BLUE_SERVER}" > target_server.txt
                    fi
                    cat target_server.txt  # Debug: Show the value
                '''
                // Set TARGET_SERVER globally
                sh "echo 'TARGET_SERVER=$(cat target_server.txt)' > env_vars"
                load 'env_vars'
                echo "TARGET_SERVER set to: ${env.TARGET_SERVER}"
            }
        }
        
        stage('Deploy to Dev') {
            steps {
                echo 'Sending app to EC2'
                withCredentials([sshUserPrivateKey(credentialsId: "${SSH_KEY_ID}", keyFileVariable: 'SSH_KEY')]) {
                    sh 'echo "SSH_KEY path: $SSH_KEY"'  // Debug: Show key file path
                    sh 'ls -l $SSH_KEY'                 // Debug: Check key file permissions
                    sh """
                        ssh -i \$SSH_KEY -v -o StrictHostKeyChecking=no ubuntu@\${TARGET_SERVER} 'mkdir -p /home/ubuntu/app'
                        scp -i \$SSH_KEY -o StrictHostKeyChecking=no ${PUBLISH_DIR}/* ubuntu@\${TARGET_SERVER}:/home/ubuntu/app/
                        ssh -i \$SSH_KEY -v -o StrictHostKeyChecking=no ubuntu@\${TARGET_SERVER} 'cd /home/ubuntu/app && nohup dotnet ProNet.Api.dll &'
                    """
                }
            }
        }
    }
    
    post {
        always {
            echo 'Cleaning up'
            cleanWs()
        }
    }
}
